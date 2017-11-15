using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class SubscribeMessageExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageRecieved))
			.Use<SubscriptionExceptionMiddleware>(new SubscriptionExceptionOptions { InnerPipe = p => p
				.Use<BodyDeserializationMiddleware>()
				.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageDeserialized))
				.Use<SubscribeInvocationMiddleware>()})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.HandlerInvoked))
			.Use<ExplicitAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageAcknowledged));

		public static readonly Action<IPipeBuilder> SubscribePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.Initialized))
			.Use<SubscriptionConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueBound))
			.Use<ChannelCreationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerChannelCreated))
			.Use<ConsumerCreationMiddleware>()
			.Use<ConsumerMessageHandlerMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<ConsumerConsumeMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod, Action<ISubscribeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.SubscribeAsync<TMessage>(async message =>
			{
				await subscribeMethod(message);
				return new Ack();
			}, context, ct);
		}

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task<Acknowledgement>> subscribeMethod, Action<ISubscribeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(
				SubscribePipe,
				ctx =>
				{
					Func<object[], Task<Acknowledgement>> genericHandler = args => subscribeMethod((TMessage) args[0]);

					ctx.Properties.TryAdd(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.TryAdd(PipeKey.MessageHandler, genericHandler);
					context?.Invoke(new SubscribeContext(ctx));
				}, ct);
		}
	}
}	
