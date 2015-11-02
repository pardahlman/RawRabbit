using System;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;

namespace RawRabbit.vNext
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config)
		{
			var addCfg = new Action<IServiceCollection>(s => s.AddSingleton<RawRabbitConfiguration>(p => config));
			var provider = new ServiceCollection()
				.AddRawRabbit(null, addCfg)
				.BuildServiceProvider();
			return CreateDefault((IServiceProvider) provider);
		}

		public static BusClient CreateDefault(Action<IServiceCollection> services = null)
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(null, services)
				.BuildServiceProvider();
			return CreateDefault((IServiceProvider) provider);
		}

		public static BusClient CreateDefault(IConfigurationRoot config, Action<IServiceCollection> services)
		{
			
			var provider = new ServiceCollection().AddRawRabbit(config, services)
				.BuildServiceProvider();
			return CreateDefault(provider);
		}

		public static BusClient CreateDefault(IServiceProvider serviceProvider)
		{
			LogManager.CurrentFactory = serviceProvider.GetService<ILoggerFactory>();

			return new BusClient(
				serviceProvider.GetService<IConfigurationEvaluator>(),
				serviceProvider.GetService<ISubscriber<MessageContext>>(),
				serviceProvider.GetService<IPublisher>(),
				serviceProvider.GetService<IResponder<MessageContext>>(),
				serviceProvider.GetService<IRequester>()
			);
		}

		public static BusClient CreateDefault(TimeSpan requestTimeout)
		{
			return CreateDefault(new RawRabbitConfiguration { RequestTimeout = requestTimeout });
		}
	}
}
