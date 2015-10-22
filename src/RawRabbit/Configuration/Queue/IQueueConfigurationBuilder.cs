namespace RawRabbit.Configuration.Queue
{
	public interface IQueueConfigurationBuilder
	{
		IQueueConfigurationBuilder WithName(string queueName);
		IQueueConfigurationBuilder WithAutoDelete(bool autoDelete = true);
		IQueueConfigurationBuilder WithDurability(bool durable = true);
		IQueueConfigurationBuilder WithExclusivity(bool exclusive = true);
		IQueueConfigurationBuilder WithArgument(string key, object value);
		
	}
}
