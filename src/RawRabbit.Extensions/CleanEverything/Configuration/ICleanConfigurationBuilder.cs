namespace RawRabbit.Extensions.CleanEverything.Configuration
{
	public interface ICleanConfigurationBuilder
	{
		ICleanConfigurationBuilder RemoveQueues();
		ICleanConfigurationBuilder RemoveExchanges();
		ICleanConfigurationBuilder CloseConnections();
	}

	public class CleanConfigurationBuilder : ICleanConfigurationBuilder
	{
		public CleanConfiguration Configuration { get; }

		public CleanConfigurationBuilder()
		{
			Configuration = new CleanConfiguration();
		}

		public ICleanConfigurationBuilder RemoveQueues()
		{
			Configuration.RemoveQueues = true;
			return this;
		}

		public ICleanConfigurationBuilder RemoveExchanges()
		{
			Configuration.RemoveExchanges = true;
			return this;
		}

		public ICleanConfigurationBuilder CloseConnections()
		{
			Configuration.CloseConnections = true;
			return this;
		}
	}
}
