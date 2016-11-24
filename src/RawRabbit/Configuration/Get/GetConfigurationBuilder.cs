namespace RawRabbit.Configuration.Get
{
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