using System;
using RabbitMQ.Client;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publish
{
	public interface IPublishConfigurationBuilder
	{
		IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
	}
}