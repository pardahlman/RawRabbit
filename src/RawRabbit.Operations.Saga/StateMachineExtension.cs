using System;
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
		public static Task RegisterStateMachineAsync<TSaga, TTriggerConfiguration>(this IBusClient busClient, Func<TSaga, Task> trigger  = null) where TSaga : Saga where TTriggerConfiguration : TriggerConfiguration, new()
		{
			return busClient.InvokeAsync(
				builder => builder
					.Use<ConsumeTriggerMiddleware>()
					.Use<TopologyMiddleware>()
					.Use<ConsumerTriggerMiddleware>()
					,
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, typeof(TSaga));
					context.Properties.Add(SagaKey.TriggerConfiguration, new TTriggerConfiguration());

				});
		}

		public static Task TriggerStateMachineAsync<TSaga>(this IBusClient busClient, Func<TSaga, Task> triggerFunc, Guid sagaId = default(Guid))
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
