using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Respond
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
			Configuration = new ResponderConfiguration();
		}

		public IResponderConfigurationBuilder WithReplyQueue(string replyTo)
		{
			Configuration.ReplyTo = replyTo;
			return this;
		}

		public IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(_exchangeBuilder);
			return this;
		}

		public IResponderConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(_queueBuilder);
			return this;
		}
	}
}