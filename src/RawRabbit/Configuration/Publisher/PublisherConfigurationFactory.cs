using System;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfigurationFactory : IPublisherConfigurationFactory
	{
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly INamingConventions _conventions;

		public PublisherConfigurationFactory(IExchangeConfigurationFactory exchange, INamingConventions conventions)
		{
			_exchange = exchange;
			_conventions = conventions;
		}

		public PublisherConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public PublisherConfiguration Create(Type messageType)
		{
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			var routingKey = _conventions.RoutingKeyConvention(messageType);
			return Create(exchangeName, routingKey);
		}

		public PublisherConfiguration Create(string exchangeName, string routingKey)
		{
			return new PublisherConfiguration
			{
				Exchange = _exchange.Create(exchangeName),
				RoutingKey = routingKey
			};
		}
	}
}