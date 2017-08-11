using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Subscribe
{
	public class SubscriptionConfigurationBuilder : ISubscriptionConfigurationBuilder
	{
		public SubscriptionConfiguration Configuration => new SubscriptionConfiguration
		{
			Queue = _queueBuilder.Configuration,
			Exchange = _exchangeBuilder.Configuration,
			RoutingKey = _routingKey ?? _queueBuilder.Configuration.QueueName,
			AutoAck = _autoAck,
			PrefetchCount = _prefetchCount == 0 ? (ushort)50 : _prefetchCount
		};

		private readonly ExchangeConfigurationBuilder _exchangeBuilder;
		private readonly QueueConfigurationBuilder _queueBuilder;
		private string _routingKey;
		private ushort _prefetchCount;
		private bool _autoAck;

		public SubscriptionConfigurationBuilder() : this(null, null, null)
		{ }

		public SubscriptionConfigurationBuilder(QueueConfiguration initialQueue, ExchangeConfiguration initialExchange, string routingKey)
		{
			_exchangeBuilder = new ExchangeConfigurationBuilder(initialExchange);
			_queueBuilder = new QueueConfigurationBuilder(initialQueue);
			_routingKey = routingKey;
		}

		public ISubscriptionConfigurationBuilder WithRoutingKey(string routingKey)
		{
			_routingKey = routingKey;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithPrefetchCount(ushort prefetchCount)
		{
			_prefetchCount = prefetchCount;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithNoAck(bool noAck = true)
		{
			return WithAutoAck(noAck);
		}

		public ISubscriptionConfigurationBuilder WithAutoAck(bool autoAck = true)
		{
			_autoAck = autoAck;
			return this;
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

		public ISubscriptionConfigurationBuilder WithSubscriberId(string subscriberId)
		{
			_queueBuilder.WithNameSuffix(subscriberId);
			return this;
		}
	}
}
