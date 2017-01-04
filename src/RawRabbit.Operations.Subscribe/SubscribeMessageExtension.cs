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
			.Use<AutoAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.HandlerInvoked));

		public static readonly Action<IPipeBuilder> AutoAckPipe = pipe => pipe
			.Use<ConsumeConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumeConfigured))
			.Use<ExecutionIdRoutingMiddleware>()
			.Use<QueueDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueBound))
			.Use<ConsumerMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerChannelCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();

		public static readonly Action<IPipeBuilder> ExplicitAckPipe = AutoAckPipe + (pipe => pipe
			.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe + (builder => builder.Replace<AutoAckMiddleware, ExplicitAckMiddleware>())
			})
		);

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod, Action<IConsumerConfigurationBuilder> configuration = null, Action<IPipeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(
				AutoAckPipe,
				ctx =>
				{
					Func<object[], Task> genericHandler = args => subscribeMethod((TMessage) args[0]);

					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
					ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
					context?.Invoke(ctx);
				}, ct);
		}

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task<Acknowledgement>> subscribeMethod, Action<IConsumerConfigurationBuilder> configuration = null)
		{
			return client.InvokeAsync(
				ExplicitAckPipe,
				context =>
				{
					Func<object[], Task> genericHandler = args => subscribeMethod((TMessage) args[0]);

					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context.Properties.Add(PipeKey.ConfigurationAction, configuration);
				}
			);
		}
	}
}	
