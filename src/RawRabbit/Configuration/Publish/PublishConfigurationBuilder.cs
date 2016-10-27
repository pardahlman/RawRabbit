using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Request;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfigurationBuilder : IPublishConfigurationBuilder
	{
		private readonly ExchangeConfigurationBuilder _exchange;
		private string _routingKey;
		private Action<IBasicProperties> _properties;
		private Action<BasicReturnEventArgs> _callback;
		private const string _oneOrMoreWords = "#";

		public PublishConfiguration Configuration => new PublishConfiguration
		{
			Exchange = _exchange.Configuration,
			RoutingKey = _routingKey,
			PropertyModifier = _properties ?? (b => {}),
			ReturnCallback = (sender, args) => _callback?.Invoke(args)
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

		public IPublishConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
		{
			_callback = callback;
			return this;
		}
	}
}