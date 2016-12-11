using System;
using RawRabbit.Context;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;

namespace RawRabbit.Operations.MessageSequence
{
	public static class MessageSequenceExtension
	{
		public static MessageSequence<TCompleteType> ExecuteSequence<TMessageContext, TCompleteType>(
			this IBusClient client,
			Func<IMessageChainPublisher<TMessageContext>, MessageSequence<TCompleteType>> cfg
		) where TMessageContext : IMessageContext, new()
		{
			var sequenceSaga = new StateMachine.MessageSequence<TMessageContext>(client);
			return cfg(sequenceSaga);
		}
	}
}
