using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Subscribe
{
	public interface ISubscriptionConfigurationBuilder
	{
		ISubscriptionConfigurationBuilder WithRoutingKey(string routingKey);
		ISubscriptionConfigurationBuilder WithPrefetchCount(ushort prefetchCount);
		ISubscriptionConfigurationBuilder WithNoAck(bool noAck = true);
		ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);

		/// <summary>
		/// The unique identifier for the subscriber. Note that the subscriber id must be
		/// unique for each routing key in order to get true topic behaviour on exchanges
		/// that supports that.
		/// </summary>
		/// <param name="subscriberId">The unique indeiindetifier for the subscriber.</param>
		/// <returns></returns>
		ISubscriptionConfigurationBuilder WithSubscriberId(string subscriberId);
	}
}
