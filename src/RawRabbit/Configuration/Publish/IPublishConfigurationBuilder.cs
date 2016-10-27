using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publish
{
	public interface IPublishConfigurationBuilder
	{
		IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublishConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback);
	}
}