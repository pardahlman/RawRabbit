using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Respond
{
	class ResponderConfigurationBuilder : IResponderConfigurationBuilder
	{
		private readonly ExchangeConfigurationBuilder _exchangeBuilder;
		private readonly QueueConfigurationBuilder _queueBuilder;

		public ResponderConfiguration Configuration { get; }

		public ResponderConfigurationBuilder(QueueConfiguration defaultQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			_exchangeBuilder = new ExchangeConfigurationBuilder(defaultExchange);
			_queueBuilder = new QueueConfigurationBuilder(defaultQueue);
			Configuration = new ResponderConfiguration
			{
				Queue = _queueBuilder.Configuration,
				Exchange = _exchangeBuilder.Configuration,
				RoutingKey = _queueBuilder.Configuration.QueueName,
				NoAck = true //TODO: Move to config.
			};
		}

		public IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(_exchangeBuilder);
			Configuration.Exchange = _exchangeBuilder.Configuration;
			return this;
		}

		public IResponderConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(_queueBuilder);
			Configuration.Queue = _queueBuilder.Configuration;
			if (string.IsNullOrEmpty(Configuration.RoutingKey))
			{
				Configuration.RoutingKey = _queueBuilder.Configuration.QueueName;
			}
			return this;
		}

		public IResponderConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}
	}
}