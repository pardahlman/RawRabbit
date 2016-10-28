using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Pipe;

namespace RawRabbit.vNext.Pipe
{
	public static class RawRabbitFactory
	{
		public static IBusClient Create(RawRabbitOptions options = null)
		{
			var collection = new ServiceCollection().AddRawRabbit();
			options?.DependencyInjection?.Invoke(collection);

			if (options?.Plugins != null)
			{
				var clientBuilder = new ClientBuilder();
				options.Plugins(clientBuilder);
				clientBuilder.ServiceAction(collection);
				collection.AddSingleton<IPipeBuilder, PipeBuilder>();
				collection.AddSingleton(clientBuilder.PipeBuilderAction);
			}
			if (options?.Configuration != null)
			{
				var builder = new ConfigurationBuilder();
				options.Configuration.Invoke(builder);
				var mainCfg = RawRabbitConfiguration.Local;
				builder.Build().Bind(mainCfg);
				mainCfg.Hostnames = mainCfg.Hostnames.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
				collection.AddSingleton(c => mainCfg);
			}
			var provider = collection.BuildServiceProvider();
			var pipeBuliderFactory = new PipeBuilderFactory(() => new PipeBuilder(provider));
			return new BusClient(pipeBuliderFactory, provider.GetService<IPipeContextFactory>());
		}
	}

	public class RawRabbitOptions
	{
		public Action<IConfigurationBuilder> Configuration { get; set; }
		public Action<IServiceCollection> DependencyInjection { get; set; }
		public Action<IClientBuilder> Plugins { get; set; }
	}
}
