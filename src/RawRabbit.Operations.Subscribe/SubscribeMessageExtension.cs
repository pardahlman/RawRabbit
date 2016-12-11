using System;
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
		private static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageRecieved))
			.Use<BodyDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageDeserialized))
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderKey = PropertyHeaders.GlobalExecutionId,
				Type = typeof(string),
				ContextSaveAction = (ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id)
			})
			.Use<GlobalExecutionIdMiddleware>()
			.Use<SubscribeInvokationMiddleware>()
			.Use<AutoAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.HandlerInvoked));

		public static readonly Action<IPipeBuilder> AutoAckPipe = pipe => pipe
			.Use<ConsumeConfigurationMiddleware>()
			.Use<ExecutionIdRoutingMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumeConfigured))
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

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod, Action<IConsumerConfigurationBuilder> configuration = null)
		{
			return client.InvokeAsync(
				AutoAckPipe,
				context =>
				{
					Func<object[], Task> genericHandler = args => subscribeMethod((TMessage) args[0]);

					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context.Properties.Add(PipeKey.ConfigurationAction, configuration);
				}
			);
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
