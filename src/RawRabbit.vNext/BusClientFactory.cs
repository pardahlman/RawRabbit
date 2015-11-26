using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Logging;

namespace RawRabbit.vNext
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(TimeSpan requestTimeout)
		{
			return CreateDefault(new RawRabbitConfiguration { RequestTimeout = requestTimeout });
		}

		public static BusClient CreateDefault(RawRabbitConfiguration config)
		{
			var addCfg = new Action<IServiceCollection>(s => s.AddSingleton(p => config));
			var services = new ServiceCollection().AddRawRabbit(null, addCfg);
			return CreateDefault(services);
		}

		public static BusClient CreateDefault(Action<IServiceCollection> custom = null)
		{
			var services = new ServiceCollection().AddRawRabbit(null, custom);
			return CreateDefault(services);
		}

		public static BusClient CreateDefault(IConfigurationRoot config, Action<IServiceCollection> custom)
		{
			var services = new ServiceCollection().AddRawRabbit(config, custom);
			return CreateDefault(services);
		}

		public static BusClient CreateDefault(IServiceCollection services)
		{
			var serviceProvider = services.BuildServiceProvider();

			LogManager.CurrentFactory = serviceProvider.GetService<ILoggerFactory>();

			return serviceProvider.GetService<BusClient>();
		}
	}
}
