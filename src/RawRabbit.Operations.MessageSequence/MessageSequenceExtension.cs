using System;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;

namespace RawRabbit.Operations.MessageSequence
{
	public static class MessageSequenceExtension
	{
		public static MessageSequence<TCompleteType> ExecuteSequence<TMessageContext, TCompleteType>(
			this IBusClient client,
			Func<IMessageChainPublisher<TMessageContext>, MessageSequence<TCompleteType>> cfg
		) where TMessageContext : new()
		{
			var sequenceMachine = new StateMachine.MessageSequence<TMessageContext>(client);
			return cfg(sequenceMachine);
		}
	}
}
