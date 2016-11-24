using System;
using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Consume
{
	public class ConsumeConfigurationFactory : IConsumeConfigurationFactory
	{
		private INamingConventions _conventions;

		public ConsumeConfigurationFactory(INamingConventions conventions)
		{
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
				QueueName = queueName,
				ExchangeName = exchangeName,
				RoutingKey = routingKey,
				ConsumerTag = Guid.NewGuid().ToString(),
				Arguments = new Dictionary<string, object>(),
			};
		}
	}
}