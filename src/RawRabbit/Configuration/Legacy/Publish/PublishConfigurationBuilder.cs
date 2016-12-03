using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Request;

namespace RawRabbit.Configuration.Legacy.Publish
{
	public class PublishConfigurationBuilder : IPublishConfigurationBuilder
	{
		private readonly ExchangeDeclarationBuilder _exchange;
		private string _routingKey;
		private Action<IBasicProperties> _properties;
		private const string _oneOrMoreWords = "#";
		private EventHandler<BasicReturnEventArgs> _basicReturn;

		public PublishConfiguration Configuration => new PublishConfiguration
		{
			Exchange = _exchange.Declaration,
			RoutingKey = _routingKey,
			PropertyModifier = _properties ?? (b => { }),
			BasicReturn = _basicReturn
		};

		public PublishConfigurationBuilder(ExchangeDeclaration defaultExchange = null, string routingKey = null)
		{
			_exchange = new ExchangeDeclarationBuilder(defaultExchange);
			_routingKey = routingKey ?? _oneOrMoreWords;
		}

		public PublishConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_exchange = new ExchangeDeclarationBuilder(defaultConfig.Exchange);
		}

		public IPublishConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			exchange(_exchange);
			Configuration.Exchange = _exchange.Declaration;
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

		public IPublishConfigurationBuilder WithMandatoryDelivery(EventHandler<BasicReturnEventArgs> basicReturn)
		{
			_basicReturn = basicReturn;
			return this;
		}
	}
}