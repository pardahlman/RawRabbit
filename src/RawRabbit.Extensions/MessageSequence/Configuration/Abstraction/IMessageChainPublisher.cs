using System;
using RawRabbit.Context;

namespace RawRabbit.Extensions.MessageSequence.Configuration.Abstraction
{
	public interface IMessageChainPublisher<TMessageContext> where TMessageContext : IMessageContext
	{
		IMessageSequenceBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new();
	}
}