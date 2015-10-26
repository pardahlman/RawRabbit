using System;
using Microsoft.Framework.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Operations.Contracts;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config)
		{
			var addCfg = new Action<IServiceCollection>(s => s.AddSingleton<RawRabbitConfiguration>(p => config));
			var provider = new ServiceCollection()
				.AddRawRabbit(addCfg)
				.BuildServiceProvider();
			return CreateDefault(provider);
		}

		public static BusClient CreateDefault(Action<IServiceCollection> services = null)
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(services)
				.BuildServiceProvider();
			return CreateDefault(provider);
		}

		public static BusClient CreateDefault(IServiceProvider serviceProvider)
		{
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
