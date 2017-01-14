using System;
using System.Threading.Tasks;

namespace RawRabbit.Operations.MessageSequence.Configuration.Abstraction
{
	public interface IMessageSequenceBuilder<TMessageContext>
	{
		IMessageSequenceBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null);
		Model.MessageSequence<TMessage> Complete<TMessage>();
	}
}