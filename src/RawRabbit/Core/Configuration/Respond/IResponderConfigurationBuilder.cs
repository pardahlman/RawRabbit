using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Respond
{
	public interface IResponderConfigurationBuilder
	{
		IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IResponderConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);
		IResponderConfigurationBuilder WithRoutingKey(string routingKey);
	}
}
