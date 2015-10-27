using System;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder WithRoutingKey(string routingKey);
		IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo);
	}
}