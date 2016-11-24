using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Legacy.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Operations.Abstraction
{
	public interface ISubscriber<out TMessageContext> where TMessageContext : IMessageContext
	{
		ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config);
	}
}