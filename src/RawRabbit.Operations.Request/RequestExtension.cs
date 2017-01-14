using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RequestExtension
	{
		public static readonly Action<IPipeBuilder> RequestPipe = pipe => pipe
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.Initialized))
				.Use<GlobalExecutionIdMiddleware>()
				.Use<RequestConfigurationMiddleware>()
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PublishConfigured))
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ConsumeConfigured))
				.Use<QueueDeclareMiddleware>(new QueueDeclareOptions {QueueDeclarationFunc = context => context.GetResponseQueue()})
				.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions {ExchangeFunc = context => context.GetRequestExchange()})
				.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions {ExchangeFunc = context => context.GetResponseExchange()})
				.Use<QueueBindMiddleware>(new QueueBindOptions
				{
					ExchangeNameFunc = context => context.GetConsumeConfiguration()?.ExchangeName,
					QueueNameFunc = context => context.GetConsumeConfiguration()?.QueueName,
					RoutingKeyFunc = context => context.GetConsumeConfiguration()?.RoutingKey
				})
				.Use<BodySerializationMiddleware>()
				.Use<Operations.Request.Middleware.BasicPropertiesMiddleware>(new BasicPropertiesOptions
				{
					PostCreateAction = (ctx, props) =>
					{
						props.Type = ctx.GetRequestMessageType().GetUserFriendlyName();
						props.Headers.TryAdd(PropertyHeaders.Sent, DateTime.UtcNow.ToString("u"));
						props.Headers.TryAdd(PropertyHeaders.GlobalExecutionId, ctx.GetGlobalExecutionId());
					}
				})
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.BasicPropertiesCreated))
				.Use<RequestTimeoutMiddleware>()
				.Use<ResponseConsumeMiddleware>(new ResponseConsumerOptions
				{
					ResponseRecieved = p => p
						.Use<BodyDeserializationMiddleware>(new MessageDeserializationOptions
						{
							BodyTypeFunc = c => Type.GetType(c.GetDeliveryEventArgs()?.BasicProperties?.Type ?? string.Empty, false),
							PersistAction = (ctx, msg) => ctx.Properties.TryAdd(RequestKey.ResponseMessage, msg)
						})
						.Use<ResponderExceptionMiddleware>()
				})
				.Use<BasicPublishMiddleware>(new BasicPublishOptions
				{
					ExchangeNameFunc = c => c.GetRequestConfiguration()?.Request.ExchangeName,
					RoutingKeyFunc = c => c.GetRequestConfiguration()?.Request.RoutingKey,
					ChannelFunc = c => c.Get<IBasicConsumer>(PipeKey.Consumer)?.Model,
					BodyFunc = c => Encoding.UTF8.GetBytes(c.Get<string>(PipeKey.SerializedMessage))
				});

		public static Task<TResponse> RequestAsync<TRequest, TResponse>(this IBusClient client, TRequest message = default(TRequest), Action<IPipeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client
				.InvokeAsync(RequestPipe, ctx =>
				{
					ctx.Properties.Add(RequestKey.RequestMessageType, typeof(TRequest));
					ctx.Properties.Add(RequestKey.ResponseMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.Message, message);
					context?.Invoke(ctx);
				}, ct)
				.ContinueWith(tContext =>
				{
					if (tContext.IsFaulted)
					{
						throw tContext?.Exception?.InnerException ?? new Exception();
					}
					return tContext.Result.Get<TResponse>(RequestKey.ResponseMessage);
				}, ct);
		}
	}
}
