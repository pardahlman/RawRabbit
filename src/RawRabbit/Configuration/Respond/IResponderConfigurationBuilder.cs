using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Respond
{
	public interface IResponderConfigurationBuilder
	{
		IResponderConfigurationBuilder WithPrefetchCount(ushort count);
		IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IResponderConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);
		IResponderConfigurationBuilder WithRoutingKey(string routingKey);
		IResponderConfigurationBuilder WithNoAck(bool noAck);
	}
}
