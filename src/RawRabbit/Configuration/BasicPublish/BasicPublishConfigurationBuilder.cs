using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationBuilder : IBasicPublishConfigurationBuilder
	{
		public BasicPublishConfiguration Configuration { get; }

		public BasicPublishConfigurationBuilder(BasicPublishConfiguration initial)
		{
			Configuration = initial;
		}

		public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithBody(byte[] body)
		{
			Configuration.Body = body;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithBody(string body)
		{
			Configuration.Body = UTF8Encoding.UTF8.GetBytes(body);
		}

		public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
		{
			Configuration.Mandatory = mandatory;
			return this;
		}

		public IBasicPublishConfigurationBuilder WithProperty(Action<IBasicProperties> propAction)
		{
			Configuration.BasicProperties = Configuration.BasicProperties ?? new BasicProperties();
			propAction?.Invoke(Configuration.BasicProperties);
			return this;
		}
	}
}