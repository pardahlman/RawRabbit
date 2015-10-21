using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private readonly QueueConfigurationBuilder _queue;
		private readonly QueueConfigurationBuilder _replyQueue;
		private readonly ExchangeConfigurationBuilder _exchange;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(QueueConfiguration defaultQueue = null, QueueConfiguration replyQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			_queue = new QueueConfigurationBuilder(defaultQueue);
			_replyQueue = new QueueConfigurationBuilder(replyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultExchange);
			Configuration = new RequestConfiguration
			{
				Queue = _queue.Configuration,
				Exchange = _exchange.Configuration,
				ReplyQueue = _replyQueue.Configuration
			};
		}

		public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_queue = new QueueConfigurationBuilder(defaultConfig.Queue);
			_replyQueue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
			Configuration = new RequestConfiguration
			{
				Queue = _queue.Configuration,
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

		public IRequestConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(_queue);
			Configuration.Queue = _queue.Configuration;
			return this;
		}

		public IRequestConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IRequestConfigurationBuilder WithReplyQueue(string replyTo)
		{
			WithReplyQueue(q => q.WithName(replyTo));
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