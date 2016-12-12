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
					.Use<RepeatMiddleware>(new RepeatOptions
					{
						EnumerableFunc = context => context.GetTriggerInvokers().OfType<MessageTriggerInvoker>(),
						RepeatContextFactory = (context, factory, invoker) => factory.CreateContext(
								new KeyValuePair<string, object>(SagaKey.TriggerInvoker, invoker),
								new KeyValuePair<string, object>(SagaKey.SagaType, typeof(TSaga)),
								new KeyValuePair<string, object>(PipeKey.MessageType, (invoker as MessageTriggerInvoker)?.MessageType),
								new KeyValuePair<string, object>(PipeKey.ConfigurationAction, ((MessageTriggerInvoker)invoker).ConfigurationAction)),
						RepeatePipe = pipe => pipe
							.Use<ConsumeConfigurationMiddleware>()
							.Use<QueueDeclareMiddleware>()
							.Use<ExchangeDeclareMiddleware>()
							.Use<QueueBindMiddleware>()
							.Use<ConsumerMiddleware>()
							.Use<MessageConsumeMiddleware>(new ConsumeOptions
							{
								Pipe = c => c
								.Use<BodyDeserializationMiddleware>()
								.Use<TriggerMessageInvokationMiddleware>()
								.Use<AutoAckMiddleware>()
							})
					}),
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, typeof(TSaga));
					context.Properties.Add(SagaKey.TriggerInvokers, new TTriggerConfiguration().GetTriggerInvokers());
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
						if (sagaId != Guid.Empty)
						{
							context.Properties.Add(SagaKey.SagaId, sagaId);
						}
					});
		}
	}
}
