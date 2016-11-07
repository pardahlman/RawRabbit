using System;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Modules;
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

namespace RawRabbit.DependencyInjection.Ninject
{
	public class RawRabbitModule : RawRabbitModule<MessageContext> { }

	public class RawRabbitModule<TMessageContext> : NinjectModule where TMessageContext : IMessageContext
	{
		public override void Load()
		{
			Kernel
				.Bind<ISubscriber<TMessageContext>>()
				.To<Subscriber<TMessageContext>>()
				.InSingletonScope();

			Kernel
				.Bind<IPublisher>()
				.To<Publisher<TMessageContext>>()
				.InSingletonScope();

			Kernel
				.Bind<IResponder<TMessageContext>>()
				.To<Responder<TMessageContext>>()
				.InSingletonScope();

			Kernel
				.Bind<IRequester>()
				.To<Requester<TMessageContext>>()
				.InSingletonScope();

			Kernel
				.Bind<IMessageContextProvider<TMessageContext>>()
				.To<MessageContextProvider<TMessageContext>>()
				.InSingletonScope()
				.WithConstructorArgument("createContextAsync", (Func<Task<TMessageContext>>)null);

			Kernel
				.Bind<Newtonsoft.Json.JsonSerializer>()
				.ToMethod(context => new Newtonsoft.Json.JsonSerializer
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver(),
					ObjectCreationHandling = ObjectCreationHandling.Auto,
					TypeNameHandling = TypeNameHandling.Objects,
					TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
				})
				.InSingletonScope();

			Kernel
				.Bind<IContextEnhancer>()
				.To<ContextEnhancer>()
				.InSingletonScope();

			Kernel
				.Bind<ITopologyProvider>()
				.To<TopologyProvider>()
				.InSingletonScope();

			Kernel
				.Bind<IConnectionFactory>()
				.ToMethod(context =>
				{
					var cfg = context.Kernel.Get<RawRabbitConfiguration>();
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
						ClientProperties = context.Kernel.Get<IClientPropertyProvider>().GetClientProperties(cfg),
						Ssl = cfg.Ssl
					};
				})
				.InSingletonScope();

			Kernel
				.Bind<IClientPropertyProvider>()
				.To<ClientPropertyProvider>()
				.InSingletonScope();

			Kernel
				.Bind<ILoggerFactory>()
				.To<LoggerFactory>()
				.InSingletonScope();

			Kernel
				.Bind<IMessageSerializer>()
				.To<JsonMessageSerializer>()
				.InSingletonScope()
				.WithConstructorArgument("config", (Action<Newtonsoft.Json.JsonSerializer>)null);

			Kernel
				.Bind<IRawConsumerFactory>()
				.To<EventingBasicConsumerFactory>()
				.InSingletonScope();

			Kernel
				.Bind<IErrorHandlingStrategy>()
				.To<DefaultStrategy>()
				.InSingletonScope();

			Kernel
				.Bind<IBasicPropertiesProvider>()
				.To<BasicPropertiesProvider>()
				.InSingletonScope();

			Kernel
				.Bind<IChannelFactory>()
				.To<ChannelFactory>()
				.InSingletonScope();

			Kernel
				.Bind<ChannelFactoryConfiguration>()
				.ToConstant(ChannelFactoryConfiguration.Default)
				.InSingletonScope();

			Kernel
				.Bind<IConfigurationEvaluator>()
				.To<ConfigurationEvaluator>()
				.InSingletonScope();

			Kernel
				.Bind<IPublishAcknowledger>()
				.To<PublishAcknowledger>()
				.InSingletonScope()
				.WithConstructorArgument("publishTimeout",
					context => context.Kernel.Get<RawRabbitConfiguration>().PublishConfirmTimeout);

			Kernel
				.Bind<INamingConventions>()
				.To<NamingConventions>()
				.InSingletonScope();

			Kernel
				.Bind<ILegacyBusClient<TMessageContext>>()
				.To<BaseBusClient<TMessageContext>>()
				.InSingletonScope();

			if (typeof(TMessageContext) == typeof(MessageContext))
			{
				Kernel
					.Bind<ILegacyBusClient>()
					.To<LegacyBusClient>()
					.InSingletonScope();
			}
		}
	}
}
