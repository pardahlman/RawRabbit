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

        public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
        {
            _replyQueue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
            _exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
            Configuration = defaultConfig ?? new RequestConfiguration();
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

        public IRequestConfigurationBuilder WithNoAck(bool noAck)
        {
            Configuration.NoAck = noAck;
            return this;
        }
    }
}