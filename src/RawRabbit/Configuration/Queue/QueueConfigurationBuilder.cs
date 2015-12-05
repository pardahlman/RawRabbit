namespace RawRabbit.Configuration.Queue
{
	public class QueueConfigurationBuilder : IQueueConfigurationBuilder
	{
		public QueueConfiguration Configuration { get;}

		public QueueConfigurationBuilder(QueueConfiguration initialQueue = null)
		{
			Configuration = initialQueue ?? QueueConfiguration.Default;
		}

		public IQueueConfigurationBuilder WithName(string queueName)
		{
			Configuration.QueueName = queueName;
			return this;
		}

		public IQueueConfigurationBuilder WithNameSuffix(string suffix)
		{
			Configuration.NameSuffix = suffix;
			return this;
		}

		public IQueueConfigurationBuilder WithAutoDelete(bool autoDelete = true)
		{
			Configuration.AutoDelete = autoDelete;
			return this;
		}

		public IQueueConfigurationBuilder WithDurability(bool durable = true)
		{
			Configuration.Durable = durable;
			return this;
		}

		public IQueueConfigurationBuilder WithExclusivity(bool exclusive = true)
		{
			Configuration.Exclusive = exclusive;
			return this;
		}

		public IQueueConfigurationBuilder WithArgument(string key, object value)
		{
			Configuration.Arguments.Add(key, value);
			return this;
		}
	}
}
