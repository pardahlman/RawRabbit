using System;

namespace RawRabbit.Operations.MessageSequence.Configuration.Abstraction
{
	public interface IMessageChainPublisher<TMessageContext>
	{
		IMessageSequenceBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new();
	}
}