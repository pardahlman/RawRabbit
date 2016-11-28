using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfigurationBuilder : IPublisherConfigurationBuilder
	{
		public PublisherConfiguration Config { get; }

		public PublisherConfigurationBuilder(PublisherConfiguration initial)
		{
			Config = initial;
		}

		public IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			var builder = new ExchangeConfigurationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Configuration;
			return this;
		}

		public IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
		{
			Config.MandatoryCallback = Config.MandatoryCallback ?? (a => { });
			Config.MandatoryCallback += callback;
			return this;
		}

		public IBasicPublishConfigurationBuilder OnExchange(string exchange)
		{
			Config.Exchange = null;
			Config.ExchangeName = exchange;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
		{
			Config.Mandatory = mandatory;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
		{
			if (Config.BasicProperties == null)
			{
				Config.BasicProperties = new BasicProperties();
			}
			propAction?.Invoke(Config.BasicProperties);
			return this;
		}
	}
}