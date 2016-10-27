using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.vNext.Disposable;

namespace RawRabbit.vNext
{
	public class BusClientFactory
	{
		public static Disposable.ILegacyBusClient CreateDefault(TimeSpan requestTimeout)
		{
			var cfg = RawRabbitConfiguration.Local;
			cfg.RequestTimeout = requestTimeout;
			return CreateDefault(cfg);
		}

		public static Disposable.ILegacyBusClient CreateDefault(RawRabbitConfiguration config)
		{
			var addCfg = new Action<IServiceCollection>(s => s.AddSingleton(p => config));
			var services = new ServiceCollection().AddRawRabbit(null, addCfg);
			return CreateDefault(services);
		}

		public static Disposable.ILegacyBusClient CreateDefault(Action<IServiceCollection> custom = null)
		{
			var services = new ServiceCollection().AddRawRabbit(null, custom);
			return CreateDefault(services);
		}

		public static Disposable.ILegacyBusClient CreateDefault(Action<IConfigurationBuilder> config, Action<IServiceCollection> custom)
		{
			var services = new ServiceCollection().AddRawRabbit(config, custom);
			return CreateDefault(services);
		}

		public static Disposable.ILegacyBusClient CreateDefault(IServiceCollection services)
		{
			var serviceProvider = services.BuildServiceProvider();
			var client = serviceProvider.GetService<ILegacyBusClient>();
			return new Disposable.LegacyBusClient(client);
		}

		public static Disposable.ILegacyBusClient<TMessageContext> CreateDefault<TMessageContext>(Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			var serviceProvider = new ServiceCollection()
				.AddRawRabbit<TMessageContext>(config, custom)
				.BuildServiceProvider();

			var client = serviceProvider.GetService<ILegacyBusClient<TMessageContext>>();
			return new LegacyBusClient<TMessageContext>(client);
		}
	}
}
