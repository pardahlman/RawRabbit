using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Publish
{
	public interface IPublishConfigurationBuilder
	{
		IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> replyTo);
	}
}