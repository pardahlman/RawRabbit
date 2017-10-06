using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Operations.Request.Context;
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
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ProducerInitialized))
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
						props.Headers.TryAdd(PropertyHeaders.Sent, DateTime.UtcNow.ToString("O"));
					}
				})
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.BasicPropertiesCreated))
				.Use<RequestTimeoutMiddleware>()
				.Use<ResponseConsumeMiddleware>(new ResponseConsumerOptions
				{
					ResponseRecieved = p => p
						.Use<ResponderExceptionMiddleware>()
						.Use<BodyDeserializationMiddleware>(new MessageDeserializationOptions
						{
							BodyTypeFunc = c => c.GetResponseMessageType(),
							PersistAction = (ctx, msg) => ctx.Properties.TryAdd(RequestKey.ResponseMessage, msg)
						})
				})
				.Use<BasicPublishMiddleware>(new BasicPublishOptions
				{
					ExchangeNameFunc = c => c.GetRequestConfiguration()?.Request.ExchangeName,
					RoutingKeyFunc = c => c.GetRequestConfiguration()?.Request.RoutingKey,
					ChannelFunc = c => c.Get<IBasicConsumer>(PipeKey.Consumer)?.Model,
					BodyFunc = c => c.Get<byte[]>(PipeKey.SerializedMessage)
				});

		public static async Task<TResponse> RequestAsync<TRequest, TResponse>(this IBusClient client, TRequest message = default(TRequest), Action<IRequestContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			var result = await client
				.InvokeAsync(RequestPipe, ctx =>
				{
					ctx.Properties.Add(RequestKey.OutgoingMessageType, typeof(TRequest));
					ctx.Properties.Add(RequestKey.IncommingMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.Message, message);
					context?.Invoke(new RequestContext(ctx));
				}, ct);
			return result.Get<TResponse>(RequestKey.ResponseMessage);
		}
	}
}
