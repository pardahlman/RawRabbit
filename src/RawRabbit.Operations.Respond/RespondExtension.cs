using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Context;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageReceived))
			.Use<RespondExceptionMiddleware>(new RespondExceptionOptions { InnerPipe = p => p
				.Use<BodyDeserializationMiddleware>(new MessageDeserializationOptions
				{
					BodyTypeFunc = context => context.GetRequestMessageType()
				})
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageDeserialized))
				.Use<HandlerInvocationMiddleware>(ResponseHandlerOptionFactory.Create()) })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.HandlerInvoked))
			.Use<ExplicitAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageAcknowledged))
			.Use<BasicPropertiesMiddleware>(new BasicPropertiesOptions
			{
				PropertyModier = (context, properties) =>
				{
					properties.CorrelationId = context.GetDeliveryEventArgs()?.BasicProperties.CorrelationId;
					properties.Type = context.GetResponseMessage()?.GetType().GetUserFriendlyName();
				}
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.BasicPropertiesCreated))
			.Use<BodySerializationMiddleware>(new MessageSerializationOptions
			{
				MessageFunc = ctx => ctx.Get<object>(RespondKey.ResponseMessage),
				PersistAction = (ctx, msg) => ctx.Properties.TryAdd(RespondKey.SerializedResponse, msg)
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponseSerialized))
			.Use<ReplyToExtractionMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ReplyToExtracted))
			.Use<PooledChannelMiddleware>(new PooledChannelOptions{ PoolNameFunc = c => RespondKey.ChannelPoolName})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.RespondChannelCreated))
			.Use<BasicPublishMiddleware>(new BasicPublishOptions
			{
				ExchangeNameFunc = context => context.GetPublicationAddress()?.ExchangeName,
				RoutingKeyFunc = context => context.GetPublicationAddress()?.RoutingKey,
				MandatoryFunc = context => true,
				BodyFunc = context => context.Get<byte[]>(RespondKey.SerializedResponse)
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponsePublished));

		public static readonly Action<IPipeBuilder> RespondPipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.Initialized))
			.Use<RespondConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueBound))
			.Use<ConsumerCreationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerCreated))
			.Use<ConsumerMessageHandlerMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<ConsumerConsumeMiddleware>()
			.Use<SubscriptionMiddleware>();

		public static Task RespondAsync<TRequest, TResponse>(
			this IBusClient client,
			Func<TRequest, Task<TResponse>> handler,
			Action<IRespondContext> context = null,
			CancellationToken ct = default(CancellationToken))
		{
			return client.RespondAsync<TRequest, TResponse>(async request =>
				{
					var response = await handler(request);
					return new Ack<TResponse>(response);
				},
				context,
				ct);
		}

		public static Task RespondAsync<TRequest, TResponse>(
			this IBusClient client,
			Func<TRequest, Task<TypedAcknowlegement<TResponse>>> handler,
			Action<IRespondContext> context = null,
			CancellationToken ct = default(CancellationToken))
		{
			return client
				.InvokeAsync(
					RespondPipe,
					ctx =>
					{
						Func<object[], Task<Acknowledgement>> genericHandler = args => handler((TRequest)args[0])
							.ContinueWith(tResponse =>
							{
								if (tResponse.IsFaulted)
									throw tResponse.Exception;
								return tResponse.Result.AsUntyped();
							}, ct);

						ctx.Properties.Add(RespondKey.IncomingMessageType, typeof(TRequest));
						ctx.Properties.Add(RespondKey.OutgoingMessageType, typeof(TResponse));
						ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context?.Invoke(new RespondContext(ctx));
					}, ct
				);
		}
	}
}
