using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public interface IPublisherConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublisherConfigurationBuilder WithRoutingKey(string routingKey);
		IPublisherConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback);
	}
}
