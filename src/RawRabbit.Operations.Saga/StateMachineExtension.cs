using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class StateMachineExtension
	{
		public static Task RegisterStateMachineAsync<TSaga, TTriggerConfiguration>(this IBusClient busClient) where TSaga : Saga where TTriggerConfiguration : TriggerConfiguration, new()
		{
			return busClient.InvokeAsync(
				builder => builder
//					.Use<ConsumeTriggerMiddleware>()
//					.Use<TopologyMiddleware>()
					.Use<RepeatMiddleware>(new RepeateOptions
					{
						EnumerableFunc = context => context.GetExternalTriggers().OfType<MessageTypeTrigger>(),
						RepeatContextFactory = (context, factory, trigger) => factory.CreateContext(
								new KeyValuePair<string, object>(PipeKey.MessageType, ((MessageTypeTrigger)trigger).MessageType),
								new KeyValuePair<string, object>(PipeKey.ConfigurationAction, ((MessageTypeTrigger)trigger).ConfigurationAction)),
						RepeatePipe = pipe => pipe
							.Use<ConsumeConfigurationMiddleware>()
							.Use<QueueDeclareMiddleware>()
							.Use<ExchangeDeclareMiddleware>()
							.Use<QueueBindMiddleware>()
					})
					.Use<ConsumerTriggerMiddleware>()
					,
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, typeof(TSaga));
					context.Properties.Add(SagaKey.ExternalTriggers, new TTriggerConfiguration().ConfigureTriggers());

				});
		}

		public static Task TriggerStateMachineAsync<TSaga>(this IBusClient busClient, Func<TSaga, Task> triggerFunc, Guid sagaId = default(Guid)) where TSaga : Saga
		{
			Func<object[], Task> genericHandler = objects => triggerFunc((TSaga) objects[0]);

			return busClient.InvokeAsync(
					builder => builder
						.Use<RetrieveSagaMiddleware>()
						.Use<MessageHandlerInvokationMiddleware>(new MessageHandlerInvokationOptions
						{
							HandlerArgsFunc = context => new object[] {context.GetSaga()},
							MessageHandlerFunc = context => context.GetMessageHandler()
						})
						.Use<PersistSagaMiddleware>()
						,
					context =>
					{
						context.Properties.Add(SagaKey.SagaType, typeof(TSaga));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context.Properties.Add(SagaKey.SagaId, sagaId);
					});
		}
	}
}
