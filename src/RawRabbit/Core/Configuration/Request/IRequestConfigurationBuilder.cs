using System;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Request
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IRequestConfigurationBuilder WithRoutingKey(string routingKey);
		IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo);
		IRequestConfigurationBuilder WithReplyQueue(string replyTo);
	}
}