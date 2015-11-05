using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Respond;
using RawRabbit.Context;

namespace RawRabbit.Operations.Contracts
{
	public interface IResponder<out TMessageContext> where TMessageContext : IMessageContext
	{
		void RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg);
	}
}