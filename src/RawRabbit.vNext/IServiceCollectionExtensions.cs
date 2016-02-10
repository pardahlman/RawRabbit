using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Consumer.Eventing;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
using RawRabbit.Context.Provider;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.vNext
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null)
		{
			return collection
				.AddTransient<IBusClient,BusClient>()
				.AddRawRabbit<MessageContext>(config, custom);
		}

		public static IServiceCollection AddRawRabbit<TMessageContext>(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			if (config != null)
			{
				var builder = new ConfigurationBuilder();
				config(builder);
				collection.AddSingleton(c => builder.Build().Get<RawRabbitConfiguration>());
			}
			else
			{
				collection.TryAddSingleton(typeof(RawRabbitConfiguration), c => RawRabbitConfiguration.Local);
			}
			

			collection
				.AddSingleton< IConnectionFactory, ConnectionFactory>(provider =>
				{
					var cfg = provider.GetService<RawRabbitConfiguration>();
					return new ConnectionFactory
					{
						VirtualHost = cfg.VirtualHost,
						UserName = cfg.Username,
						Password = cfg.Password,
						Port = cfg.Port,
						HostName = cfg.Hostnames.FirstOrDefault() ?? string.Empty,
						AutomaticRecoveryEnabled = cfg.AutomaticRecovery,
						TopologyRecoveryEnabled = cfg.TopologyRecovery,
						NetworkRecoveryInterval = cfg.RecoveryInterval,
						ClientProperties = provider.GetService<IClientPropertyProvider>().GetClientProperties(cfg),
						Ssl = cfg.Ssl
					};
				})
				.AddSingleton<IClientPropertyProvider, ClientPropertyProvider>()
				.AddSingleton<ILoggerFactory, LoggerFactory>()
				.AddTransient<IMessageSerializer, JsonMessageSerializer>()
				.AddTransient<IConsumerFactory, EventingBasicConsumerFactory>()
				.AddTransient<IErrorHandlingStrategy, DefaultStrategy>()
				.AddSingleton<IMessageContextProvider<TMessageContext>, MessageContextProvider<TMessageContext>>()
				.AddSingleton<IContextEnhancer, ContextEnhancer>()
				.AddSingleton<IBasicPropertiesProvider, BasicPropertiesProvider>()
				.AddSingleton<IChannelFactory, ChannelFactory>()
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<IPublishAcknowledger, PublishAcknowledger>(
					p => new PublishAcknowledger(p.GetService<RawRabbitConfiguration>().PublishConfirmTimeout)
				)
				.AddSingleton<INamingConventions, NamingConventions>()
				.AddTransient<ISubscriber<TMessageContext>, Subscriber<TMessageContext>>()
				.AddTransient<IPublisher, Publisher<TMessageContext>>()
				.AddTransient<IResponder<TMessageContext>, Responder<TMessageContext>>()
				.AddTransient<IRequester, Requester<TMessageContext>>(
					p => new Requester<TMessageContext>(
						p.GetService<IChannelFactory>(),
						p.GetService<IConsumerFactory>(),
						p.GetService<IMessageSerializer>(),
						p.GetService<IMessageContextProvider<TMessageContext>>(),
						p.GetService<IErrorHandlingStrategy>(),
						p.GetService<IBasicPropertiesProvider>(),
						p.GetService<RawRabbitConfiguration>().RequestTimeout))
				.AddTransient<IBusClient<TMessageContext>, BaseBusClient<TMessageContext>>();
			custom?.Invoke(collection);
			return collection;
		}
	}
}
