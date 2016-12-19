using System;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Trigger;
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
					.Use<RepeatMiddleware>(new RepeatOptions
					{
						EnumerableFunc = context => context.GetSagaSubscriberOptions(),
						RepeatContextFactory = (context, factory, enumerated) =>
						{
							var options = enumerated as SagaSubscriberOptions;
							var childContext = factory.CreateContext(context.Properties.ToArray());
							childContext.Properties.TryAdd(SagaKey.PipeBuilderAction, options?.PipeActionFunc(childContext));
							childContext.Properties.TryAdd(SagaKey.ContextAction, options?.ContextActionFunc(childContext));
							return childContext;
						},
						RepeatePipe = pipe => pipe.Use<SagaSubscriberMiddleware>()
					}),
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, typeof(TSaga));
					context.Properties.Add(SagaKey.SagaSubscriberOptions, new TTriggerConfiguration().GetSagaSubscriberOptions());
				});
		}

		public static Task TriggerStateMachineAsync<TSaga>(this IBusClient busClient, Func<TSaga, Task> triggerFunc, Guid sagaId = default(Guid)) where TSaga : Saga
		{
			Func<object[], Task> genericHandler = objects => triggerFunc((TSaga) objects[0]);

			return busClient
				.InvokeAsync(
					builder => builder
						.Use<RetrieveSagaMiddleware>()
						.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
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
						if (sagaId != Guid.Empty)
						{
							context.Properties.Add(SagaKey.SagaId, sagaId);
						}
					}
				);
		}
	}
}
