using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Logging;
using RawRabbit.vNext.Pipe;

namespace RawRabbit.IntegrationTests
{
	public static class RawRabbitFactory
	{
		public static Instantiation.Disposable.BusClient CreateTestClient(RawRabbitOptions options = null)
		{
			return vNext.Pipe.RawRabbitFactory.CreateSingleton(GetTestOptions(options));
		}

		public static Instantiation.InstanceFactory CreateTestInstanceFactory(RawRabbitOptions options = null)
		{
			return vNext.Pipe.RawRabbitFactory.CreateInstanceFactory(GetTestOptions(options));
		}

		private static RawRabbitOptions GetTestOptions(RawRabbitOptions options)
		{
			options = options ?? new RawRabbitOptions();
			var action = options.DependencyInjection ?? (collection => { });
			action += collection =>
			{
				var registration = collection.LastOrDefault(c => c.ServiceType == typeof(RawRabbitConfiguration));
				var prevRegged = registration?.ImplementationInstance as RawRabbitConfiguration ?? registration?.ImplementationFactory(null) as RawRabbitConfiguration;
				if (prevRegged != null)
				{
					prevRegged.Queue.AutoDelete = true;
					prevRegged.Exchange.AutoDelete = true;
					collection.AddSingleton(p => prevRegged);
				}
				LogManager.CurrentFactory = new VoidLoggerFactory();
			};
			options.DependencyInjection = action;
			return options;
		}
	}
}
