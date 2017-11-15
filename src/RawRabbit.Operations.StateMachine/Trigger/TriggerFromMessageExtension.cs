using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.StateMachine.Context;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public static class TriggerFromMessageExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe =
			SubscribeMessageExtension.ConsumePipe + (p => p
				.Replace<SubscriptionExceptionMiddleware, SubscriptionExceptionMiddleware>(args: new SubscriptionExceptionOptions
				{
					InnerPipe = inner => inner
						.Use<BodyDeserializationMiddleware>()
						.Use<ModelIdMiddleware>()
						.Use<GlobalLockMiddleware>()
						.Use<RetrieveStateMachineMiddleware>()
						.Use<HandlerInvocationMiddleware>(new HandlerInvocationOptions
						{
							HandlerArgsFunc = context => context.GetLazyHandlerArgs()
						})
				})
			);

		public static readonly Action<IPipeBuilder> SubscribePipe =
			SubscribeMessageExtension.SubscribePipe + (p => p
				.Replace<ConsumerMessageHandlerMiddleware, ConsumerMessageHandlerMiddleware>(args: new ConsumeOptions {Pipe = ConsumePipe})
			);

		public static TriggerConfigurer FromMessage<TStateMachine, TMessage>(
			this TriggerConfigurer configurer,
			Func<TMessage, Guid> correlationFunc,
			Func<TStateMachine, TMessage, Task> machineFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null
		)
		{
			Func<object[], Task<Acknowledgement>> genericHandler = args => machineFunc((TStateMachine)args[0], (TMessage)args[1]).ContinueWith<Acknowledgement>(t => new Ack());
			Func<object[], Guid> genericCorrFunc = args => correlationFunc((TMessage)args[0]);

			return configurer.From(SubscribePipe, context =>
			{
				var stateMachineContext = new StateMachineContext(context);
				stateMachineContext.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
				stateMachineContext.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
				stateMachineContext.UseLazyCorrelationArgs(ctx => new[] { ctx.GetMessage()});
				stateMachineContext.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				stateMachineContext.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
				stateMachineContext.Properties.Add(PipeKey.MessageHandler, genericHandler);
				stateMachineContext.UseLazyHandlerArgs(ctx => new[] { ctx.GetStateMachine(), ctx.GetMessage() });
			});
		}

		public static TriggerConfigurer FromMessage<TStateMachine, TMessage>(
			this TriggerConfigurer configurer,
			Func<TMessage, Guid> correlationFunc,
			Action<TStateMachine, TMessage> stateMachineAction,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			return configurer.FromMessage<TStateMachine, TMessage>(
				correlationFunc, (machine, message) =>
				{
					stateMachineAction(machine, message);
					return Task.FromResult(0);
				},
				consumeConfig);
		}
	}
}
