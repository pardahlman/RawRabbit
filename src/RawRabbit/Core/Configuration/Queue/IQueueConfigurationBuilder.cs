namespace RawRabbit.Core.Configuration
{
	public interface IQueueConfigurationBuilder
	{
		IQueueConfigurationBuilder WithName(string queueName);
		IQueueConfigurationBuilder WithRoutingKey(string routingKey);
	}
}
