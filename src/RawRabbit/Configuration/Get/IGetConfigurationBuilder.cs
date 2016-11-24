namespace RawRabbit.Configuration.Get
{
	public interface IGetConfigurationBuilder
	{
		IGetConfigurationBuilder FromQueue(string queueName);
		IGetConfigurationBuilder WithNoAck(bool noAck = true);
	}
}
