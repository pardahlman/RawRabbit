using RawRabbit.Configuration;
using RawRabbit.Instantiation;

namespace RawRabbit.IntegrationTests
{
	public static class RawRabbitFactory
	{
		public static Instantiation.Disposable.BusClient CreateTestClient(RawRabbitOptions options = null)
		{
			return Instantiation.RawRabbitFactory.CreateSingleton(GetTestOptions(options));
		}

		public static InstanceFactory CreateTestInstanceFactory(RawRabbitOptions options = null)
		{
			return Instantiation.RawRabbitFactory.CreateInstanceFactory(GetTestOptions(options));
		}

		private static RawRabbitOptions GetTestOptions(RawRabbitOptions options)
		{
			options = options ?? new RawRabbitOptions();
			options.ClientConfiguration = options.ClientConfiguration ?? RawRabbitConfiguration.Local;
			options.ClientConfiguration.Queue.AutoDelete = true;
			options.ClientConfiguration.Exchange.AutoDelete = true;

			return options;
		}
	}
}
