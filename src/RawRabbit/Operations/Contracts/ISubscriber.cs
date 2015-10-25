using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Operations.Contracts
{
	public interface ISubscriber<out TMessageContext> where TMessageContext : IMessageContext
	{
		Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config);
	}
}