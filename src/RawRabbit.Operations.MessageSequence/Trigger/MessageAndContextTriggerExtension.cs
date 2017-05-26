using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.MessageSequence.Trigger
{
	public static class MessageAndContextTriggerExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe =
			SubscribeMessageContextExtension.ConsumePipe + (p => p
				.Replace<SubscriptionExceptionMiddleware, SubscriptionExceptionMiddleware>(args: new SubscriptionExceptionOptions
				{
					InnerPipe = inner => inner
						.Use<ModelIdMiddleware>()
						.Use<GlobalLockMiddleware>()
						.Use<RetrieveStateMachineMiddleware>()
						.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
						{
							HandlerArgsFunc = context => context.GetLazyHandlerArgs()
						})
				})
			);

		public static readonly Action<IPipeBuilder> SubscribePipe =
			SubscribeMessageExtension.SubscribePipe + (p => p
				.Replace<ConsumerMessageHandlerMiddleware, ConsumerMessageHandlerMiddleware>(args: new ConsumeOptions {Pipe = ConsumePipe})
			);

		public static TriggerConfigurer FromMessage<TStateMachine, TMessage, TMessageContext>(
			this TriggerConfigurer configurer,
			Func<TMessage, TMessageContext, Guid> correlationFunc,
			Func<TStateMachine, TMessage, TMessageContext, Task> machineFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null
		)
		{
			Func<object[], Task> genericHandler = args => 
				machineFunc((TStateMachine) args[0], (TMessage) args[1], (TMessageContext)args[2])
					.ContinueWith<Acknowledgement>(t => new Ack());
			Func<object, object, Guid> genericCorrFunc = (msg, ctx) => correlationFunc((TMessage) msg, (TMessageContext)ctx);

			return configurer.From(SubscribePipe,context =>
			{
				context.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
				context.AddMessageContextType<TMessageContext>();
				context.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
				context.UseLazyCorrelationArgs(ctx => new[] { ctx.GetMessage(), ctx.GetMessageContext() });
				context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				context.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
				context.Properties.Add(PipeKey.MessageHandler, genericHandler);
				context.UseLazyHandlerArgs(ctx => new[] { ctx.GetStateMachine(), ctx.GetMessage(), ctx.GetMessageContext() });
			});
		}

		public static TriggerConfigurer FromMessage<TStateMachine, TMessage, TMessageContext>(
			this TriggerConfigurer configurer,
			Func<TMessage, TMessageContext, Guid> correlationFunc,
			Action<TStateMachine, TMessage, TMessageContext> stateMachineAction,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			return configurer.FromMessage<TStateMachine, TMessage, TMessageContext>(
				correlationFunc, (machine, message, context) =>
				{
					stateMachineAction(machine, message, context);
					return Task.FromResult(0);
				},
				consumeConfig);
		}
	}
}
