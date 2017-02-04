using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public class TriggerConfigurer<TStateMachine> where TStateMachine : StateMachineBase
	{
		public List<TriggerPipeOptions> TriggerPipeOptions { get; set; }

		public static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<BodyDeserializationMiddleware>()
			.Use<ModelIdMiddleware>()
			.Use<GlobalLockMiddleware>()
			.Use<RetrieveStateMachineMiddleware>()
			.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
			{
				HandlerArgsFunc = context => new[] { context.GetStateMachine(), context.GetMessage() }
			})
			.Use<AutoAckMiddleware>();

		public static readonly Action<IPipeBuilder> AutoAckPipe = SubscribeMessageExtension.SubscribePipe + (builder => builder
			.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe
			}));

		public TriggerConfigurer()
		{
			TriggerPipeOptions = new List<TriggerPipeOptions>();
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

			TriggerPipeOptions.Add(new TriggerPipeOptions
			{
				ContextActionFunc = c=>  context =>
				{
					context.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
				},
				PipeActionFunc = c =>  AutoAckPipe
			});
			return this;
		}
	}
}
