using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private readonly QueueDeclarationBuilder _replyQueue;
		private readonly ExchangeDeclarationBuilder _exchange;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_replyQueue = new QueueDeclarationBuilder(defaultConfig.ReplyQueue);
			_exchange = new ExchangeDeclarationBuilder(defaultConfig.Exchange);
			Configuration = defaultConfig ?? new RequestConfiguration();
		}

		public IRequestConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			exchange(_exchange);
			Configuration.Exchange = _exchange.Declaration;
			return this;
		}

		public IRequestConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IRequestConfigurationBuilder WithReplyQueue(Action<IQueueDeclarationBuilder> replyTo)
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