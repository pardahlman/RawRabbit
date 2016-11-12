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
			options = options ?? new RawRabbitOptions();
			var action = options.DependencyInjection ?? (collection => {});
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

				collection.AddSingleton<ILoggerFactory, VoidLoggerFactory>();
			};
			options.DependencyInjection = action;
			return vNext.Pipe.RawRabbitFactory.CreateSingleton(options);
		}
	}
}
