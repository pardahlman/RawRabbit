using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Legacy.Publish
{
	public interface IPublishConfigurationBuilder
	{
		IPublishConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublishConfigurationBuilder WithMandatoryDelivery(EventHandler<BasicReturnEventArgs> basicReturn);
	}
}