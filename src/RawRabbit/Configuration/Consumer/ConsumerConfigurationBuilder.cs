using System;
using System.Collections.Generic;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfigurationBuilder : IConsumerConfigurationBuilder
	{
		public ConsumerConfiguration Config { get; }

		public ConsumerConfigurationBuilder(ConsumerConfiguration initial)
		{
			Config = initial;
		}

		public IConsumerConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			var builder = new ExchangeConfigurationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Configuration;
			return this;
		}

		public IConsumerConfigurationBuilder FromDeclaredQueue(Action<IQueueConfigurationBuilder> queue)
		{
			var builder = new QueueConfigurationBuilder(Config.Queue);
			queue(builder);
			Config.Queue = builder.Configuration;
			return this;
		}

		public IConsumerConfigurationBuilder WithNoAck(bool noAck = true)
		{
			Config.NoAck = noAck;
			return this;
		}

		public IConsumerConfigurationBuilder WithConsumerTag(string tag)
		{
			Config.ConsumerTag = tag;
			return this;
		}

		public IConsumerConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IConsumerConfigurationBuilder WithNoLocal(bool noLocal = true)
		{
			Config.NoLocal = noLocal;
			return this;
		}

		public IConsumerConfigurationBuilder WithPrefetchCount(ushort prefetch)
		{
			Config.PrefetchCount = prefetch;
			return this;
		}

		public IConsumerConfigurationBuilder WithExclusive(bool exclusive = true)
		{
			Config.Exclusive = exclusive;
			return this;
		}

		public IConsumerConfigurationBuilder WithArgument(string key, object value)
		{
			Config.Arguments = Config.Arguments ?? new Dictionary<string, object>();
			Config.Arguments.TryAdd(key, value);
			return this;
		}
	}
}