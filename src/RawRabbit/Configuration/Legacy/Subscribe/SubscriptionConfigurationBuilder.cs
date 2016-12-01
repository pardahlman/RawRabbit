using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Subscribe
{
	public class SubscriptionConfigurationBuilder : ISubscriptionConfigurationBuilder
	{
		public SubscriptionConfiguration Configuration => new SubscriptionConfiguration
		{
			Queue = _queueBuilder.Configuration,
			Exchange = _exchangeBuilder.Declaration,
			RoutingKey = _routingKey ?? _queueBuilder.Configuration.Name,
			NoAck = _noAck,
			PrefetchCount = _prefetchCount == 0 ? (ushort)50 : _prefetchCount
		};

		private readonly ExchangeDeclarationBuilder _exchangeBuilder;
		private readonly QueueDeclarationBuilder _queueBuilder;
		private string _routingKey;
		private ushort _prefetchCount;
		private bool _noAck;

		public SubscriptionConfigurationBuilder() : this(null, null, null)
		{ }

		public SubscriptionConfigurationBuilder(QueueDeclaration initialQueue, ExchangeDeclaration initialExchange, string routingKey)
		{
			_exchangeBuilder = new ExchangeDeclarationBuilder(initialExchange);
			_queueBuilder = new QueueDeclarationBuilder(initialQueue);
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
			_noAck = noAck;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			exchange(_exchangeBuilder);
			return this;
		}

		public ISubscriptionConfigurationBuilder WithQueue(Action<IQueueDeclarationBuilder> queue)
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
