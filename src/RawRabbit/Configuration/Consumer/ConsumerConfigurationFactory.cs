using System;
using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfigurationFactory : IConsumerConfigurationFactory
	{
		private readonly IQueueConfigurationFactory _queue;
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly INamingConventions _conventions;

		public ConsumerConfigurationFactory(IQueueConfigurationFactory queue, IExchangeConfigurationFactory exchange, INamingConventions conventions)
		{
			_queue = queue;
			_exchange = exchange;
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
			return new ConsumerConfiguration
			{
				Queue = _queue.Create(queueName),
				Exchange = _exchange.Create(exchangeName),
				RoutingKey = routingKey,
				Arguments = new Dictionary<string, object>(),
			};
		}
	}
}