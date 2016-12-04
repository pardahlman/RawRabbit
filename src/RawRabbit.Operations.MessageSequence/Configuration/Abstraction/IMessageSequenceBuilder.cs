using System;
using System.Threading.Tasks;
using RawRabbit.Context;

namespace RawRabbit.Operations.MessageSequence.Configuration.Abstraction
{
	public interface IMessageSequenceBuilder<TMessageContext> where TMessageContext : IMessageContext
	{
		IMessageSequenceBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null);
		Model.MessageSequence<TMessage> Complete<TMessage>();
	}
}