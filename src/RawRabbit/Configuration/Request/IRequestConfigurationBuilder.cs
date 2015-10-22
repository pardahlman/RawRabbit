using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IRequestConfigurationBuilder WithRoutingKey(string routingKey);
		IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo);
	}
}