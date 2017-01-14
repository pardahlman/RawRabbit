using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;

namespace RawRabbit.Compatibility.Legacy.Configuration.Publish
{
	public interface IPublishConfigurationBuilder
	{
		IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublishConfigurationBuilder WithMandatoryDelivery(EventHandler<BasicReturnEventArgs> basicReturn);
	}
}