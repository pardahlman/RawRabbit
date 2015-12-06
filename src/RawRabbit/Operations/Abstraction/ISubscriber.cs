using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Operations.Abstraction
{
	public interface ISubscriber<out TMessageContext> where TMessageContext : IMessageContext
	{
		void SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config);
	}
}