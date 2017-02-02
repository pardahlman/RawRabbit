using System;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;

namespace RawRabbit.Operations.MessageSequence
{
	public static class MessageSequenceExtension
	{
		public static MessageSequence<TCompleteType> ExecuteSequence<TCompleteType>(
			this IBusClient client,
			Func<IMessageChainPublisher, MessageSequence<TCompleteType>> cfg
		)
		{
			var sequenceMachine = new StateMachine.MessageSequence(client);
			return cfg(sequenceMachine);
		}
	}
}
