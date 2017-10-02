using System;

namespace RawRabbit.Configuration.Consumer
{
	public interface IConsumerConfigurationFactory
	{
		ConsumerConfiguration Create<TMessageType>();
		ConsumerConfiguration Create(Type messageType);
		ConsumerConfiguration Create(string queueName, string exchangeName, string routingKey);
	}
}