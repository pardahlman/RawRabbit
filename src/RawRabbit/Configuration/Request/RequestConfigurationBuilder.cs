using System;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private readonly QueueConfigurationBuilder _replyQueue;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			_replyQueue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
			Configuration = defaultConfig;
		}
	
		public IRequestConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}

		public IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo)
		{
			replyTo(_replyQueue);
			Configuration.ReplyQueue = _replyQueue.Configuration;
			return this;
		}
	}
}