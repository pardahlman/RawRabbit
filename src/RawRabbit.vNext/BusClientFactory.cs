using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;

namespace RawRabbit.vNext
{
	public class BusClientFactory
	{
		public static IBusClient CreateDefault(TimeSpan requestTimeout)
		{
			var cfg = RawRabbitConfiguration.Local;
			cfg.RequestTimeout = requestTimeout;
			return CreateDefault(cfg);
		}

		public static IBusClient CreateDefault(RawRabbitConfiguration config)
		{
			var addCfg = new Action<IServiceCollection>(s => s.AddSingleton(p => config));
			var services = new ServiceCollection().AddRawRabbit(null, addCfg);
			return CreateDefault(services);
		}

		public static IBusClient CreateDefault(Action<IServiceCollection> custom = null)
		{
			var services = new ServiceCollection().AddRawRabbit(null, custom);
			return CreateDefault(services);
		}

		public static IBusClient CreateDefault(Action<IConfigurationBuilder> config, Action<IServiceCollection> custom)
		{
			var services = new ServiceCollection().AddRawRabbit(config, custom);
			return CreateDefault(services);
		}

		public static IBusClient CreateDefault(IServiceCollection services)
		{
			var serviceProvider = services.BuildServiceProvider();
			return serviceProvider.GetService<IBusClient>();
		}

		public static IBusClient<TMessageContext> CreateDefault<TMessageContext>(Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			var serviceProvider = new ServiceCollection()
				.AddRawRabbit<TMessageContext>(config, custom)
				.BuildServiceProvider();

			return serviceProvider.GetService<IBusClient<TMessageContext>>();
		}
	}
}
