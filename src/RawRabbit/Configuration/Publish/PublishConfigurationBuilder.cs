using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Request;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfigurationBuilder : IPublishConfigurationBuilder
	{
		private readonly QueueConfigurationBuilder _queue;
		private readonly ExchangeConfigurationBuilder _exchange;
		private readonly string _routingKey;

		public PublishConfiguration Configuration => new PublishConfiguration
		{
			Queue = _queue.Configuration,
			Exchange = _exchange.Configuration,
			RoutingKey = _routingKey
		};

		public PublishConfigurationBuilder(QueueConfiguration replyQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			_queue = new QueueConfigurationBuilder(replyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultExchange);
			_routingKey = _queue.Configuration.QueueName;
		}

		public PublishConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_queue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
		}

		public IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(_exchange);
			Configuration.Exchange = _exchange.Configuration;
			return this;
		}

		public IPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IPublishConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> replyTo)
		{
			replyTo(_queue);
			return this;
		}
	}
}