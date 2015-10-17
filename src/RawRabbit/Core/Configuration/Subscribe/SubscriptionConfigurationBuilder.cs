using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Subscribe
{
	public class SubscriptionConfigurationBuilder : ISubscriptionConfigurationBuilder
	{
		public SubscriptionConfiguration Configuration => new SubscriptionConfiguration
		{
			QueueConfiguration = _queueBuilder.Configuration,
			ExchangeConfiguration = _exchangeBuilder.Configuration
		};

		private readonly ExchangeConfigurationBuilder _exchangeBuilder;
		private readonly QueueConfigurationBuilder _queueBuilder;

		public SubscriptionConfigurationBuilder()
		{
			_exchangeBuilder = new ExchangeConfigurationBuilder();
			_queueBuilder = new QueueConfigurationBuilder();
		}
		public ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(_exchangeBuilder);
			return this;
		}

		public ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(_queueBuilder);
			return this;
		}
	}
}
