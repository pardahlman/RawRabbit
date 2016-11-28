namespace RawRabbit.Configuration.BasicPublish
{
	public interface IBasicPublishConfigurationFactory
	{
		BasicPublishConfiguration Create(object message);
		BasicPublishConfiguration Create();
	}
}