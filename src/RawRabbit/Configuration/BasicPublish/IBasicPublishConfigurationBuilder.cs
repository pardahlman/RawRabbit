using System;
using RabbitMQ.Client;

namespace RawRabbit.Configuration.BasicPublish
{
	public interface IBasicPublishConfigurationBuilder
	{
		IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IBasicPublishConfigurationBuilder WithBody(byte[] body);
		IBasicPublishConfigurationBuilder WithBody(string body);
		IBasicPublishConfigurationBuilder AsMandatory(bool mandatory=true);
		IBasicPublishConfigurationBuilder WithProperty(Action<IBasicProperties> propAction);
	}
}