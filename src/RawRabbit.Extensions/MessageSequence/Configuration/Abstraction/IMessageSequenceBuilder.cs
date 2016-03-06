using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Model;

namespace RawRabbit.Extensions.MessageSequence.Configuration.Abstraction
{
	public interface IMessageSequenceBuilder<TMessageContext> where TMessageContext : IMessageContext
	{
		IMessageSequenceBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null);
		MessageSequence<TMessage> Complete<TMessage>();
	}
}