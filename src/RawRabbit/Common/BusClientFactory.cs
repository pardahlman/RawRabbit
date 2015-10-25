using System;
using Microsoft.Framework.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Operations.Contracts;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config, Action<IServiceCollection> configureIoc = null)
		{
			var services = new ServiceCollection();
			services.AddRawRabbit(configureIoc);
			var serviceProvider = services.BuildServiceProvider();
			return CreateDefault(serviceProvider);
		}

		public static BusClient CreateDefault(Action<IServiceCollection> services = null)
		{
			return CreateDefault(null, services);
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
