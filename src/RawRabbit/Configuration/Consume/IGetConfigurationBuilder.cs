namespace RawRabbit.Configuration.Consume
{
	public interface IGetConfigurationBuilder
	{
		IGetConfigurationBuilder FromQueue(string queueName);
		IGetConfigurationBuilder WithNoAck(bool noAck = true);
	}

	public class GetConfiguration
	{
		public string QueueName { get; set; }
		public bool NoAck { get; set; }
	}

	public class GetConfigurationBuilder : IGetConfigurationBuilder
	{
		public GetConfiguration Configuration { get; }

		public GetConfigurationBuilder(GetConfiguration config = null)
		{
			Configuration = config ?? new GetConfiguration();
		}
		public IGetConfigurationBuilder FromQueue(string queueName)
		{
			Configuration.QueueName = queueName;
			return this;
		}

		public IGetConfigurationBuilder WithNoAck(bool noAck = true)
		{
			Configuration.NoAck = noAck;
			return this;
		}
	}
}
