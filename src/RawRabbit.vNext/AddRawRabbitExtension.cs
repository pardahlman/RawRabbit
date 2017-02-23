using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.DependecyInjection;
using RawRabbit.vNext.DependecyInjection;
using RawRabbitOptions = RawRabbit.vNext.Pipe.RawRabbitOptions;

namespace RawRabbit.vNext
{
	public static class AddRawRabbitExtension
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, RawRabbitOptions options = null)
		{
			options?.DependencyInjection?.Invoke(collection);
			if (options?.Configuration != null)
			{
				options.ClientConfiguration = CreateClientConfig(options.Configuration);
			}
			var adapter = new ServiceCollectionAdapter(collection);
			adapter.AddRawRabbit(options);
			
			return collection;
		}

		private static RawRabbitConfiguration CreateClientConfig(Action<IConfigurationBuilder> config)
		{
			var builder = new ConfigurationBuilder();
			config.Invoke(builder);
			var mainCfg = RawRabbitConfiguration.Local;
			builder.Build().Bind(mainCfg);
			mainCfg.Hostnames = mainCfg.Hostnames.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
			return mainCfg;
		}
	}
}
