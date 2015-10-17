namespace RawRabbit.Core.Configuration.Queue
{
	public class QueueConfigurationBuilder : IQueueConfigurationBuilder
	{
		public QueueConfiguration Configuration { get;}

		public QueueConfigurationBuilder()
		{
			Configuration = QueueConfiguration.Default;
		}

		public IQueueConfigurationBuilder WithName(string queueName)
		{
			Configuration.QueueName = queueName;
			return this;
		}

		public IQueueConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Configuration.RoutingKey = routingKey;
			return this;
		}
	}
}
