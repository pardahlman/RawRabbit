using System;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfigurationFactory : IConsumerConfigurationFactory
	{
		private readonly IQueueConfigurationFactory _queue;
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly IConsumeConfigurationFactory _consume;
		private readonly INamingConventions _conventions;

		public ConsumerConfigurationFactory(IQueueConfigurationFactory queue, IExchangeConfigurationFactory exchange, IConsumeConfigurationFactory consume, INamingConventions conventions)
		{
			_queue = queue;
			_exchange = exchange;
			_consume = consume;
			_conventions = conventions;
		}

		public ConsumerConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public ConsumerConfiguration Create(Type messageType)
		{
			var queueName = _conventions.QueueNamingConvention(messageType);
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			var routingKey = _conventions.RoutingKeyConvention(messageType);
			return Create(queueName, exchangeName, routingKey);
		}

		public ConsumerConfiguration Create(string queueName, string exchangeName, string routingKey)
		{
			var cfg =  new ConsumerConfiguration
			{
				Queue = _queue.Create(queueName),
				Exchange = _exchange.Create(exchangeName),
				Consume = _consume.Create(queueName, exchangeName, routingKey)
			};
			return cfg;
		}
	}
}