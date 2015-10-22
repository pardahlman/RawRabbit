using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private readonly QueueConfigurationBuilder _replyQueue;
		private readonly ExchangeConfigurationBuilder _exchange;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(QueueConfiguration defaultQueue = null, QueueConfiguration replyQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			_replyQueue = new QueueConfigurationBuilder(replyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultExchange);
			Configuration = new RequestConfiguration
			{
				Exchange = _exchange.Configuration,
				ReplyQueue = _replyQueue.Configuration
			};
		}

		public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_replyQueue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
			Configuration = new RequestConfiguration
			{
				Exchange = _exchange.Configuration,
				ReplyQueue = _replyQueue.Configuration,
				RoutingKey = defaultConfig.RoutingKey
			};
		}

		public IRequestConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(_exchange);
			Configuration.Exchange = _exchange.Configuration;
			return this;
		}

		public IRequestConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo)
		{
			replyTo(_replyQueue);
			Configuration.ReplyQueue = _replyQueue.Configuration;
			return this;
		}
	}
}