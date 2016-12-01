using System;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public interface IPublisherConfigurationBuilder : IBasicPublishConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange);
		IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback);
	}
}
