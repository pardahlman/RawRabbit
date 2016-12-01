using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Respond
{
	class ResponderConfigurationBuilder : IResponderConfigurationBuilder
	{
		private readonly ExchangeDeclarationBuilder _exchangeBuilder;
		private readonly QueueDeclarationBuilder _queueBuilder;

		public ResponderConfiguration Configuration { get; }

		public ResponderConfigurationBuilder(QueueDeclaration defaultQueue = null, ExchangeDeclaration defaultExchange = null)
		{
			_exchangeBuilder = new ExchangeDeclarationBuilder(defaultExchange);
			_queueBuilder = new QueueDeclarationBuilder(defaultQueue);
			Configuration = new ResponderConfiguration
			{
				Queue = _queueBuilder.Configuration,
				Exchange = _exchangeBuilder.Declaration,
				RoutingKey = _queueBuilder.Configuration.Name
			};
		}

		public IResponderConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			exchange(_exchangeBuilder);
			Configuration.Exchange = _exchangeBuilder.Declaration;
			return this;
		}

		public IResponderConfigurationBuilder WithPrefetchCount(ushort count)
		{
			Configuration.PrefetchCount = count;
			return this;
		}

		public IResponderConfigurationBuilder WithQueue(Action<IQueueDeclarationBuilder> queue)
		{
			queue(_queueBuilder);
			Configuration.Queue = _queueBuilder.Configuration;
			if (string.IsNullOrEmpty(Configuration.RoutingKey))
			{
				Configuration.RoutingKey = _queueBuilder.Configuration.Name;
			}
			return this;
		}

		public IResponderConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IResponderConfigurationBuilder WithNoAck(bool noAck)
		{
			Configuration.NoAck = noAck;
			return this;
		}
	}
}