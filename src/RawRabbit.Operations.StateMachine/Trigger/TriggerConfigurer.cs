using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public class TriggerConfigurer<TStateMachine> where TStateMachine : StateMachineBase
	{
		public List<Action<IPipeContext>> TriggerContextActions { get; set; }
		
		public TriggerConfigurer()
		{
			TriggerContextActions = new List<Action<IPipeContext>>();
		}

		public TriggerConfigurer<TStateMachine> From(Action<IPipeContext> context)
		{
			TriggerContextActions.Add(context);
			return this;
		}
		public TriggerConfigurer<TStateMachine> FromMessage<TMessage>(
			Func<TMessage, Guid> correlationFunc,
			Action<TStateMachine, TMessage> stateMachineAction,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			return FromMessage(
				correlationFunc, (machine, message) =>
				{
					stateMachineAction(machine, message);
					return Task.FromResult(0);
				},
				consumeConfig);
		}

		public TriggerConfigurer<TStateMachine> FromMessage<TMessage>(
			Func<TMessage, Guid> correlationFunc,
			Func<TStateMachine, TMessage, Task> machineFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			Func<object[], Task> genericHandler = args => machineFunc((TStateMachine)args[0], (TMessage)args[1]).ContinueWith<Acknowledgement>(t => new Ack());
			Func<object, Guid> genericCorrFunc = o => correlationFunc((TMessage) o);

			return From(context =>
			{
				context.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
				context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				context.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
				context.Properties.Add(PipeKey.MessageHandler, genericHandler);
				context.UseLazyHandlerArgs(ctx => new[] { ctx.GetStateMachine(), ctx.GetMessage() });
			});
		}
	}
}
