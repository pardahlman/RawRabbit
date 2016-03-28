using System.Linq;
using Autofac;
using Autofac.Core;
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
using RawRabbit.Serialization;

namespace RawRabbit.DependencyInjection.Autofac
{
	public class RawRabbitModule : RawRabbitModule<MessageContext> { }

	public class RawRabbitModule<TMessageContext> : Module where TMessageContext : IMessageContext
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.RegisterType<Subscriber<TMessageContext>>()
				.As<ISubscriber<TMessageContext>>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<Publisher<TMessageContext>>()
				.As<IPublisher>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<Responder<TMessageContext>>()
				.As<IResponder<TMessageContext>>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<Requester<TMessageContext>>()
				.As<IRequester>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<MessageContextProvider<TMessageContext>>()
				.As<IMessageContextProvider<TMessageContext>>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<ContextEnhancer>()
				.As<IContextEnhancer>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<TopologyProvider>()
				.As<ITopologyProvider>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<ContextEnhancer>()
				.As<IContextEnhancer>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.Register(context =>
					{
						var cfg = context.Resolve<RawRabbitConfiguration>();
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
							ClientProperties = context.Resolve<IClientPropertyProvider>().GetClientProperties(cfg),
							Ssl = cfg.Ssl
						};
					})
				.As<IConnectionFactory>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<ClientPropertyProvider>()
				.As<IClientPropertyProvider>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<LoggerFactory>()
				.As<ILoggerFactory>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<JsonMessageSerializer>()
				.As<IMessageSerializer>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<EventingBasicConsumerFactory>()
				.As<IConsumerFactory>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<DefaultStrategy>()
				.As<IErrorHandlingStrategy>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<BasicPropertiesProvider>()
				.As<IBasicPropertiesProvider>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<ChannelFactory>()
				.As<IChannelFactory>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.Register(c => ChannelFactoryConfiguration.Default)
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<ConfigurationEvaluator>()
				.As<IConfigurationEvaluator>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<PublishAcknowledger>()
				.WithParameter(new ResolvedParameter(
					(info, context) => info.Name == "publishTimeout",
					(info, context) => context.Resolve<RawRabbitConfiguration>().PublishConfirmTimeout))
				.As<IPublishAcknowledger>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<NamingConventions>()
				.As<INamingConventions>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<BaseBusClient<TMessageContext>>()
				.As<IBusClient<TMessageContext>>()
				.SingleInstance()
				.PreserveExistingDefaults();

			if (typeof (TMessageContext) == typeof (MessageContext))
			{
				builder
					.RegisterType<BusClient>()
					.As<IBusClient>()
					.SingleInstance()
					.PreserveExistingDefaults();
			}
		}
	}
}
