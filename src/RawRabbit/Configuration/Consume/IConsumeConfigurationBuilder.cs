using System;
using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Configuration.Consume
{
	public interface IConsumeConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IConsumeConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange);

		/// <summary>
		/// Specify the topology features of the Queue to consume from.
		/// Queue will be declared.
		/// /// </summary>
		/// <param name="queue"></param>
		/// <returns></returns>
		IConsumeConfigurationBuilder FromDeclaredQueue(Action<IQueueConfigurationBuilder> queue);
		IConsumeConfigurationBuilder WithNoAck(bool noAck = true);
		IConsumeConfigurationBuilder WithConsumerTag(string tag);
		IConsumeConfigurationBuilder WithRoutingKey(string routingKey);
		IConsumeConfigurationBuilder WithNoLocal(bool noLocal = true);
		IConsumeConfigurationBuilder WithPrefetchCount(ushort prefetch);
		IConsumeConfigurationBuilder WithExclusive(bool exclusive = true);
		IConsumeConfigurationBuilder WithArgument(string key, object value);
	}

	public class ConsumeConfiguration
	{
		public QueueConfiguration Queue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public bool NoAck { get; set; }
		public string ConsumerTag { get; set; }
		public string RoutingKey { get; set; }
		public bool NoLocal { get; set; }
		public ushort PrefetchCount { get; set; }
		public bool Exclusive { get; set; }
		public Dictionary<string, object> Arguments { get; set; }
	}

	public class ConsumeConfigurationBuilder : IConsumeConfigurationBuilder
	{
		public ConsumeConfiguration Config { get; }

		public ConsumeConfigurationBuilder(ConsumeConfiguration initial)
		{
			Config = initial;
		}

		public IConsumeConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			var builder = new ExchangeConfigurationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Configuration;
			return this;
		}

		public IConsumeConfigurationBuilder FromDeclaredQueue(Action<IQueueConfigurationBuilder> queue)
		{
			var builder = new QueueConfigurationBuilder(Config.Queue);
			queue(builder);
			Config.Queue = builder.Configuration;
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

	public interface IConsumeConfigurationFactory
	{
		ConsumeConfiguration Create<TMessageType>();
		ConsumeConfiguration Create(Type messageType);
		ConsumeConfiguration Create(string queueName, string exchangeName, string routingKey);
	}

	public class ConsumeConfigurationFactory : IConsumeConfigurationFactory
	{
		private readonly IQueueConfigurationFactory _queue;
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly INamingConventions _conventions;

		public ConsumeConfigurationFactory(IQueueConfigurationFactory queue, IExchangeConfigurationFactory exchange, INamingConventions conventions)
		{
			_queue = queue;
			_exchange = exchange;
			_conventions = conventions;
		}

		public ConsumeConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public ConsumeConfiguration Create(Type messageType)
		{
			var queueName = _conventions.QueueNamingConvention(messageType);
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			var routingKey = _conventions.RoutingKeyConvention(messageType);
			return Create(queueName, exchangeName, routingKey);
		}

		public ConsumeConfiguration Create(string queueName, string exchangeName, string routingKey)
		{
			return new ConsumeConfiguration
			{
				Queue = _queue.Create(queueName),
				Exchange = _exchange.Create(exchangeName),
				RoutingKey = routingKey,
				Arguments = new Dictionary<string, object>(),
			};
		}
	}
}