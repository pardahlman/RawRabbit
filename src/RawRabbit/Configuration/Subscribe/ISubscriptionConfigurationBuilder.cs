using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Subscribe
{
	public interface ISubscriptionConfigurationBuilder
	{
		ISubscriptionConfigurationBuilder WithRoutingKey(string routingKey);
		ISubscriptionConfigurationBuilder WithPrefetchCount(ushort prefetchCount);
		ISubscriptionConfigurationBuilder WithNoAck(bool noAck = true);
		ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);
	}
}
