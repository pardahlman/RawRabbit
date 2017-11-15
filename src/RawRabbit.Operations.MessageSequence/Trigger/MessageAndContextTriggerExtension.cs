using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Context;
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
						.Use<BodyDeserializationMiddleware>()
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageDeserialized))
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextDeserialized))
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

		public static TriggerConfigurer FromMessage<TStateMachine, TMessage, TMessageContext>(
			this TriggerConfigurer configurer,
			Func<TMessage, TMessageContext, Guid> correlationFunc,
			Func<TStateMachine, TMessage, TMessageContext, Task> machineFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null
		)
		{
			Func<object[], Task<Acknowledgement>> genericHandler = args => 
				machineFunc((TStateMachine) args[0], (TMessage) args[1], (TMessageContext)args[2])
					.ContinueWith<Acknowledgement>(t => new Ack());
			Func<object, object, Guid> genericCorrFunc = (msg, ctx) => correlationFunc((TMessage) msg, (TMessageContext)ctx);

			return configurer.From(SubscribePipe,context =>
			{
				var stateMachineContext = new StateMachineContext(context);
				stateMachineContext.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
				stateMachineContext.AddMessageContextType<TMessageContext>();
				stateMachineContext.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
				stateMachineContext.UseLazyCorrelationArgs(ctx => new[] { ctx.GetMessage(), ctx.GetMessageContext() });
				stateMachineContext.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				stateMachineContext.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
				stateMachineContext.Properties.Add(PipeKey.MessageHandler, genericHandler);
				stateMachineContext.UseLazyHandlerArgs(ctx => new[] { ctx.GetStateMachine(), ctx.GetMessage(), ctx.GetMessageContext() });
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
