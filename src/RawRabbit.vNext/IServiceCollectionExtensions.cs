using System;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
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
using RawRabbit.Pipe;
using RawRabbit.Serialization;
using RawRabbit.vNext.DependecyInjection;
using PipeBuilder = RawRabbit.vNext.Pipe.PipeBuilder;

namespace RawRabbit.vNext
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null)
		{
			return collection
				.AddSingleton<ILegacyBusClient>(provider =>
					{
						LogManager.CurrentFactory = provider.GetService<ILoggerFactory>();
						return ActivatorUtilities.CreateInstance<RawRabbit.LegacyBusClient>(provider);
					})
				.AddRawRabbit<MessageContext>(config, custom);
		}

		public static IServiceCollection AddRawRabbit<TMessageContext>(this IServiceCollection collection, Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
		{
			if (config != null)
			{
				var builder = new ConfigurationBuilder();
				config?.Invoke(builder);
				var mainCfg = RawRabbitConfiguration.Local;
				builder.Build().Bind(mainCfg);
				mainCfg.Hostnames = mainCfg.Hostnames.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

				collection.AddSingleton(c => mainCfg);
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
				.AddTransient(c => new Newtonsoft.Json.JsonSerializer
				{
					TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
					Formatting = Formatting.None,
					CheckAdditionalContent = true,
					ContractResolver = new CamelCasePropertyNamesContractResolver(),
					ObjectCreationHandling = ObjectCreationHandling.Auto,
					DefaultValueHandling = DefaultValueHandling.Ignore,
					TypeNameHandling = TypeNameHandling.All,
					ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
					MissingMemberHandling = MissingMemberHandling.Ignore,
					PreserveReferencesHandling = PreserveReferencesHandling.Objects,
					NullValueHandling = NullValueHandling.Ignore

				})
				.AddTransient<IRawConsumerFactory, EventingBasicConsumerFactory>()
				.AddTransient<IErrorHandlingStrategy, DefaultStrategy>()
				.AddSingleton<IMessageContextProvider<TMessageContext>, MessageContextProvider<TMessageContext>>()
				.AddSingleton<IContextEnhancer, ContextEnhancer>()
				.AddSingleton<IBasicPropertiesProvider, BasicPropertiesProvider>()
				.AddSingleton<IChannelFactory, ChannelFactory>()
				.AddSingleton(c => ChannelFactoryConfiguration.Default)
				.AddSingleton<ITopologyProvider, TopologyProvider>()
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<IPublishAcknowledger, PublishAcknowledger>(
					p => new PublishAcknowledger(p.GetService<RawRabbitConfiguration>().PublishConfirmTimeout)
				)
				.AddSingleton<INamingConventions, NamingConventions>()
				.AddTransient<ISubscriber<TMessageContext>, Subscriber<TMessageContext>>()
				.AddTransient<IPublisher, Publisher<TMessageContext>>()
				.AddTransient<IResponder<TMessageContext>, Responder<TMessageContext>>()
				.AddTransient<IRequester, Requester<TMessageContext>>()

				.AddSingleton<IBusClient, BusClient>()
				.AddSingleton<IResourceDisposer, ResourceDisposer>()
				.AddSingleton<IPipeContextFactory, PipeContextFactory>()
				.AddSingleton<IPipeBuilderFactory>(provider => new PipeBuilderFactory(() => new PipeBuilder(new ServiceProviderAdapter(provider))))

				.AddSingleton<ILegacyBusClient<TMessageContext>>(provider =>
				{
					LogManager.CurrentFactory = provider.GetService<ILoggerFactory>();
					return ActivatorUtilities.CreateInstance<BaseBusClient<TMessageContext>>(provider);
				});
			custom?.Invoke(collection);

			return collection;
		}
	}
}
