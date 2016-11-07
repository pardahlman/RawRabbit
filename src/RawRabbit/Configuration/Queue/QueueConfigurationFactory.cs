using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Queue
{
	public interface IQueueConfigurationFactory
	{
		QueueConfiguration Create(string queueName);
		QueueConfiguration Create<TMessageType>();
	}

	public class QueueConfigurationFactory : IQueueConfigurationFactory
	{
		private readonly RawRabbitConfiguration _config;
		private readonly INamingConventions _conventions;

		public QueueConfigurationFactory(RawRabbitConfiguration config, INamingConventions conventions)
		{
			_config = config;
			_conventions = conventions;
		}

		public QueueConfiguration Create(string queueName)
		{
			return new QueueConfiguration
			{
				AutoDelete = _config.Queue.AutoDelete,
				Durable = _config.Queue.Durable,
				Exclusive = _config.Queue.Exclusive,
				QueueName = queueName,
				Arguments = new Dictionary<string, object>()
			};
		}

		public QueueConfiguration Create<TMessageType>()
		{
			var queueName = _conventions.QueueNamingConvention(typeof(TMessageType));
			return Create(queueName);
		}
	}
}
