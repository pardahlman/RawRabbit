using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Logging;
using RawRabbit.vNext;

namespace RawRabbit.IntegrationTests
{
	public class TestClientFactory
	{
		public static vNext.Disposable.IBusClient CreateNormal(Action<IServiceCollection> action = null, Action<IConfigurationBuilder> config = null)
		{
			action = AddTestConfig(action);
			return BusClientFactory.CreateDefault(config, action);
		}

		public static vNext.Disposable.IBusClient<TMessageContext> CreateNormal<TMessageContext>(Action<IServiceCollection> action = null,
			Action<IConfigurationBuilder> config = null) where TMessageContext : IMessageContext
		{
			action = AddTestConfig(action);
			return BusClientFactory.CreateDefault<TMessageContext>(config, action);
		}

		public static RawRabbit.Extensions.Disposable.IBusClient CreateExtendable(Action<IServiceCollection> custom = null)
		{
			custom = AddTestConfig(custom);
			return RawRabbitFactory.Create(custom);
		}

		private static Action<IServiceCollection> AddTestConfig(Action<IServiceCollection> action)
		{
			action = action ?? (collection => { });
			action += collection =>
			{
				var prevRegged = collection
					.LastOrDefault(c => c.ServiceType == typeof(RawRabbitConfiguration))?
					.ImplementationFactory(null) as RawRabbitConfiguration;
				if (prevRegged != null)
				{
					prevRegged.Queue.AutoDelete = true;
					collection.AddSingleton<RawRabbitConfiguration>(p => prevRegged);
				}

				collection.AddSingleton<ILoggerFactory, VoidLoggerFactory>();
			};
			return action;
		}
	}
}
