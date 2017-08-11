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
			return WithAutoAck(noAck);
		}

		public IGetConfigurationBuilder WithAutoAck(bool autoAck = true)
		{
			Configuration.AutoAck = autoAck;
			return this;
		}
	}
}