using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Core;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Extensions.MessageSequence.Repository;
using RawRabbit.Extensions.TopologyUpdater.Core;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Extensions.Client
{
	public static class IServiceCollectionExtension
	{
		public static IServiceCollection AddRawRabbitExtensions<TMessageContext>(this IServiceCollection collection) where  TMessageContext : IMessageContext
		{
			collection
				/* Message Sequence */
				.AddSingleton<IMessageChainDispatcher, MessageChainDispatcher>()
				.AddSingleton<IMessageSequenceRepository, MessageSequenceRepository>()
				.AddSingleton<IMessageChainTopologyUtil, MessageChainTopologyUtil<TMessageContext>>()
				.AddSingleton(c =>
				{
					var chainQueue = QueueConfiguration.Default;
					chainQueue.QueueName = $"rawrabbit_chain_{Guid.NewGuid()}";
					chainQueue.AutoDelete = true;
					chainQueue.Exclusive = true;
					return chainQueue;
				})
				/* Topology Updater */
				.AddTransient<IBindingProvider, BindingProvider>()
				.AddTransient<IExchangeUpdater, ExchangeUpdater>()
				.AddSingleton<ILegacyBusClient<TMessageContext>>(c => new ExtendableBusClient<TMessageContext>(collection.BuildServiceProvider()));
			return collection;
		}

		public static IServiceCollection AddRawRabbit<TMessageContext>(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			return vNext.IServiceCollectionExtensions
				.AddRawRabbit(collection, config, custom)
				.AddRawRabbitExtensions<TMessageContext>()
				.AddSingleton<ILegacyBusClient>(c =>
				{
					LogManager.CurrentFactory = c.GetService<ILoggerFactory>();
					return new ExtendableBusClient(collection.BuildServiceProvider());
				});
		}

		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null)
		{
			return AddRawRabbit<MessageContext>(collection, config, custom)
				.AddSingleton<ILegacyBusClient>(provider =>
				{
					LogManager.CurrentFactory = provider.GetService<ILoggerFactory>();
					return new ExtendableBusClient(collection.BuildServiceProvider());
				});
		}

		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, IConfigurationSection section, Action<IServiceCollection> custom = null)
		{
			custom = custom ?? (serviceCollection => { });
			custom += ioc => ioc.AddSingleton(c =>
			{
				var mainCfg = RawRabbitConfiguration.Local;
				section.Bind(mainCfg);
				mainCfg.Hostnames = mainCfg.Hostnames.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
				return mainCfg;
			});

			return AddRawRabbit(collection, config: null, custom: custom);
		}
	}
}
