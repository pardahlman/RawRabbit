using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.vNext.DependecyInjection;

namespace RawRabbit.vNext.Pipe
{
	public static class RawRabbitFactory
	{
		public static IBusClient Create(RawRabbitOptions options = null)
		{
			var collection = new ServiceCollection();
			options?.DependencyInjection?.Invoke(collection);
			var ioc = new ServiceCollectionAdapter(collection);

			if (options?.Configuration != null)
			{
				var builder = new ConfigurationBuilder();
				options.Configuration.Invoke(builder);
				var mainCfg = RawRabbitConfiguration.Local;
				builder.Build().Bind(mainCfg);
				mainCfg.Hostnames = mainCfg.Hostnames.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
				ioc.AddSingleton(c => mainCfg);
			}
			return Instantiation.RawRabbitFactory.Create(options, ioc, register => new ServiceProviderAdapter((register as ServiceCollectionAdapter)?.Collection));
		}
	}
}
