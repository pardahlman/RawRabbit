using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
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
			.Use<RequestConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PublishConfigured))
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>(new QueueDeclareOptions { QueueFunc = context => context.GetResponseQueue()})
			.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions { ExchangeFunc = context => context.GetResponseExchange()})
			.Use<QueueBindMiddleware>(new QueueBindOptions { ConsumeFunc = context => context.GetResponseConfiguration() })
			.Use<MessageSerializationMiddleware>(new MessageSerializationOptions { MessageFunc = context => context.GetMessage()})
			.Use<Operations.Request.Middleware.BasicPropertiesMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.BasicPropertiesCreated))
			.Use<ResponseConsumeMiddleware>(new ResponseConsumerOptions
				{
					ResponseRecieved = p => p
						.Use<MessageDeserializationMiddleware>(new MessageDeserializationOptions
						{
							MessageTypeFunc = c => c.GetResponseMessageType(),
							MessageKeyFunc = c => RequestKey.ResponseMessage
						})
				})
			.Use<PublishMessage>(new PublishOptions
				{
					ExchangeFunc = c => c.GetRequestConfiguration()?.Request.Exchange.ExchangeName,
					RoutingKeyFunc = c => c.GetRequestConfiguration()?.Request.RoutingKey,
					ChannelFunc = c => c.Get<IBasicConsumer>(PipeKey.Consumer)?.Model
				})
			;

		public static Task<TResponse> RequestAsync<TRequest, TResponse>(this IBusClient client, TRequest message = default(TRequest), Action<IRequestConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(RequestPipe, ctx =>
				{
					ctx.Properties.Add(RequestKey.RequestMessageType, typeof(TRequest));
					ctx.Properties.Add(RequestKey.ResponseMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
				})
				.ContinueWith(tContext => tContext.Result.Get<TResponse>(RequestKey.ResponseMessage));
		}
	}
}
