using System.Collections.Generic;
using RawRabbit.Pipe;

namespace RawRabbit.Configuration.Consume
{
	public class ConsumeConfigurationBuilder : IConsumeConfigurationBuilder
	{
		public ConsumeConfiguration Config { get; }
		public bool ExistingExchange { get; set; }
		public bool ExistingQueue { get; set; }

		public ConsumeConfigurationBuilder(ConsumeConfiguration initial)
		{
			Config = initial;
		}

		public IConsumeConfigurationBuilder OnExchange(string exchange)
		{
			Config.ExchangeName = exchange;
			ExistingExchange = true;
			return this;
		}

		public IConsumeConfigurationBuilder FromQueue(string queue)
		{
			Config.QueueName = queue;
			ExistingQueue = true;
			return this;
		}

		public IConsumeConfigurationBuilder WithNoAck(bool noAck = true)
		{
			Config.NoAck = noAck;
			return this;
		}

		public IConsumeConfigurationBuilder WithConsumerTag(string tag)
		{
			Config.ConsumerTag = tag;
			return this;
		}

		public IConsumeConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IConsumeConfigurationBuilder WithNoLocal(bool noLocal = true)
		{
			Config.NoLocal = noLocal;
			return this;
		}

		public IConsumeConfigurationBuilder WithPrefetchCount(ushort prefetch)
		{
			Config.PrefetchCount = prefetch;
			return this;
		}

		public IConsumeConfigurationBuilder WithExclusive(bool exclusive = true)
		{
			Config.Exclusive = exclusive;
			return this;
		}

		public IConsumeConfigurationBuilder WithArgument(string key, object value)
		{
			Config.Arguments = Config.Arguments ?? new Dictionary<string, object>();
			Config.Arguments.TryAdd(key, value);
			return this;
		}
	}
}