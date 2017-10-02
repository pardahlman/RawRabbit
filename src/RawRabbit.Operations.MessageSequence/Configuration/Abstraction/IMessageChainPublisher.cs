using System;
using System.Threading;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.MessageSequence.Configuration.Abstraction
{
	public interface IMessageChainPublisher
	{
		IMessageSequenceBuilder PublishAsync<TMessage>(
			TMessage message = default(TMessage),
			Guid globalMessageId = new Guid()) where TMessage : new();

		IMessageSequenceBuilder PublishAsync<TMessage>(
			TMessage message,
			Action<IPipeContext> context,
			CancellationToken ct = default(CancellationToken)) where TMessage : new();
	}
}