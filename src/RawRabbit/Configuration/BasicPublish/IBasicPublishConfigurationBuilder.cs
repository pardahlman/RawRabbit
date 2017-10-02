using System;
using RabbitMQ.Client;

namespace RawRabbit.Configuration.BasicPublish
{
	public interface IBasicPublishConfigurationBuilder
	{
		IBasicPublishConfigurationBuilder OnExchange(string exchange);
		IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IBasicPublishConfigurationBuilder AsMandatory(bool mandatory=true);
		IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction);
	}
}