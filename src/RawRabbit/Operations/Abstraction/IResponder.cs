using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Context;

namespace RawRabbit.Operations.Abstraction
{
	public interface IResponder<out TMessageContext> where TMessageContext : IMessageContext
	{
		ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg);
	}
}