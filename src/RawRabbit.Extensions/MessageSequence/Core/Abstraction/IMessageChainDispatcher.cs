using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Model;

namespace RawRabbit.Extensions.MessageSequence.Core.Abstraction
{
	public interface IMessageChainDispatcher
	{
		Task AddMessageHandlerAsync<TMessage, TMessageContext>(Guid globalMessageId, Func<TMessage, TMessageContext, Task> func, StepOption configuration = null) where TMessageContext : IMessageContext;
		Task InvokeMessageHandlerAsync(Guid globalMessageId, object body, IMessageContext context);
	}
}