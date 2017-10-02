using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RawRabbit.Subscription
{
	public interface ISubscriptionRepository
	{
		void Add(ISubscription subscription);
		List<ISubscription> GetAll();
	}

	public class SubscriptionRepository : ISubscriptionRepository
	{
		private readonly ConcurrentBag<ISubscription> _subscriptions;

		public SubscriptionRepository()
		{
			_subscriptions = new ConcurrentBag<ISubscription>();
		}

		public void Add(ISubscription subscription)
		{
			_subscriptions.Add(subscription);
		}

		public List<ISubscription> GetAll()
		{
			return _subscriptions.ToList();
		}
	}
}
