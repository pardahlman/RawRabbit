using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
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
			.Use<BodyDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageDeserialized))
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderKeyFunc = c =>  PropertyHeaders.GlobalExecutionId,
				HeaderTypeFunc = c => typeof(string),
				ContextSaveAction = (ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id)
			})
			.Use<GlobalExecutionIdMiddleware>()
			.Use<SubscriptionExceptionMiddleware>(new SubscriptionExceptionOptions { InnerPipe = p => p.Use<SubscribeInvokationMiddleware>()})
			.Use<ExplicitAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.HandlerInvoked));

		public static readonly Action<IPipeBuilder> SubscribePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.Initialized))
			.Use<ConsumeConfigurationMiddleware>()
			.Use<ExecutionIdRoutingMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueBound))
			.Use<ConsumerMiddleware>()
			.Use<ConsumerRecoveryMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerChannelCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod, Action<IPipeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.SubscribeAsync<TMessage>(
				message => subscribeMethod
					.Invoke(message)
					.ContinueWith<Acknowledgement>(t => new Ack(), ct),
				context, ct);
		}

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task<Acknowledgement>> subscribeMethod, Action<IPipeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(
				SubscribePipe,
				ctx =>
				{
					Func<object[], Task> genericHandler = args => subscribeMethod((TMessage) args[0]);

					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context?.Invoke(ctx);
				}, ct);
		}
	}
}	
