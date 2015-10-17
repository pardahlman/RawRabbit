using System;

namespace RawRabbit.Core.Configuration.Subscribe
{
	public interface ISubscriptionConfigurationBuilder
	{
		ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);
	}
}
