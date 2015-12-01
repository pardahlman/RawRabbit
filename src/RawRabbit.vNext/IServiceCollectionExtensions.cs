using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Contract;
using RawRabbit.Consumer.Eventing;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
using RawRabbit.Context.Provider;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.vNext
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, IConfigurationRoot config = null, Action<IServiceCollection> custom = null)
		{
			return collection
				.AddTransient<BusClient>()
				.AddRawRabbit<MessageContext>(config, custom);
		}

		public static IServiceCollection AddRawRabbit<TMessageContext>(this IServiceCollection collection, IConfigurationRoot config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			collection
				.AddSingleton<RawRabbitConfiguration>(p =>
					config == null
						? new RawRabbitConfiguration()
						: p.GetService<IConfigurationParser>().Parse(config))
				.AddSingleton<IEnumerable<IConnectionFactory>>(p =>
				{
					var cfg = p.GetService<RawRabbitConfiguration>();
					var brokers = cfg?.Brokers ?? new List<BrokerConfiguration>();
					brokers = brokers.Any() ? brokers : new List<BrokerConfiguration> { BrokerConfiguration.Local };
					return brokers.Select(b => new ConnectionFactory
					{
						HostName = b.Hostname,
						VirtualHost = b.VirtualHost,
						UserName = b.Username,
						Password = b.Password,
						AutomaticRecoveryEnabled = true,
						TopologyRecoveryEnabled = true,
						ClientProperties = p.GetService<IClientPropertyProvider>().GetClientProperties(cfg ,b)
					});
				})
				.AddSingleton<IConnectionBroker, DefaultConnectionBroker>(
					p => new DefaultConnectionBroker(
						p.GetService<IEnumerable<IConnectionFactory>>(),
						TimeSpan.FromMinutes(1) //TODO: Move this to config
					)
				)
				.AddSingleton<IClientPropertyProvider, ClientPropertyProvider>()
				.AddSingleton<ILoggerFactory, LoggerFactory>()
				.AddTransient<IConfigurationParser, ConfigurationParser>()
				.AddTransient<IMessageSerializer, JsonMessageSerializer>()
				.AddTransient<IConsumerFactory, EventingBasicConsumerFactory>()
				.AddTransient<IErrorHandlingStrategy, DefaultStrategy>()
				.AddSingleton<IMessageContextProvider<TMessageContext>, MessageContextProvider<TMessageContext>>()
				.AddSingleton<IContextEnhancer, ContextEnhancer>()
				.AddSingleton<IChannelFactory, ChannelFactory>()
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<IPublishAcknowledger, PublishAcknowledger>(
					p => new PublishAcknowledger(p.GetService<RawRabbitConfiguration>().PublishConfirmTimeout)
				)
				.AddTransient<INamingConvetions, NamingConvetions>()
				.AddTransient<ISubscriber<TMessageContext>, Subscriber<TMessageContext>>()
				.AddTransient<IPublisher, Publisher<TMessageContext>>()
				.AddTransient<IResponder<TMessageContext>, Responder<TMessageContext>>()
				.AddTransient<IRequester, SingleConsumerRequester<TMessageContext>>(
					p => new SingleConsumerRequester<TMessageContext>(
						p.GetService<IChannelFactory>(),
						p.GetService<IConsumerFactory>(),
						p.GetService<IMessageSerializer>(),
						p.GetService<IMessageContextProvider<TMessageContext>>(),
						p.GetService<IErrorHandlingStrategy>(),
						p.GetService<RawRabbitConfiguration>().RequestTimeout))
				.AddTransient<IBusClient<TMessageContext>, BaseBusClient<TMessageContext>>();
			custom?.Invoke(collection);
			return collection;
		}
	}
}
