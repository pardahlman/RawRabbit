using System;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Middleware;

namespace RawRabbit.Operations.MessageSequence
{
	public static class MessageSequenceExtension
	{
		public static MessageSequence<TCompleteType> ExecuteSequence<TCompleteType>(
			this IBusClient client,
			Func<IMessageChainPublisher, MessageSequence<TCompleteType>> cfg
		)
		{
			var sequenceMachine = client
				.InvokeAsync(ctx => ctx
					.Use<RetrieveStateMachineMiddleware>(new RetrieveStateMachineOptions
					{
						StateMachineTypeFunc = pipeContext => typeof(StateMachine.MessageSequence)
					})
				)
				.GetAwaiter()
				.GetResult()
				.GetStateMachine();

			return cfg((StateMachine.MessageSequence)sequenceMachine);
		}
	}
}
