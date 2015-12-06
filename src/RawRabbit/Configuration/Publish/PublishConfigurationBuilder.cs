using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Request;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfigurationBuilder : IPublishConfigurationBuilder
	{
		private readonly ExchangeConfigurationBuilder _exchange;
		private string _routingKey;
		private Action<IBasicProperties> _properties;
		private const string _oneOrMoreWords = "#";

		public PublishConfiguration Configuration => new PublishConfiguration
		{
			Exchange = _exchange.Configuration,
			RoutingKey = _routingKey,
			PropertyModifier = _properties ?? (b => {})
		};

		public PublishConfigurationBuilder(ExchangeConfiguration defaultExchange = null, string routingKey =null)
		{
			_exchange = new ExchangeConfigurationBuilder(defaultExchange);
			_routingKey = routingKey ?? _oneOrMoreWords;
		}

		public PublishConfigurationBuilder(RequestConfiguration defaultConfig)
		{
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
			_routingKey = routingKey;
			return this;
		}

		public IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties)
		{
			_properties = properties;
			return this;
		}
	}
}