using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Subscribe
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
