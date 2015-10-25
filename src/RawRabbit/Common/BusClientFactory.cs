using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Consumer;
using RawRabbit.Consumer.Contract;
using RawRabbit.Consumer.Eventing;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config, Action<IServiceCollection> configureIoc = null)
		{
			var services = new ServiceCollection();
			services
				.AddSingleton<RawRabbitConfiguration>(provider => config ?? new RawRabbitConfiguration())
				.AddSingleton<IConnectionFactory, ConnectionFactory>(p =>
				{
					var cfg = p.GetService<RawRabbitConfiguration>();
					return new ConnectionFactory
					{
						HostName = cfg.Hostname,
						Password = cfg.Password,
						UserName = cfg.Username,
						AutomaticRecoveryEnabled = true,
						TopologyRecoveryEnabled = true,
					};
				})
				.AddTransient<IMessageSerializer, JsonMessageSerializer>()
				.AddTransient<IConsumerFactory, EventingBasicConsumerFactory>()
				.AddSingleton<IMessageContextProvider<MessageContext>, DefaultMessageContextProvider>(
					p => new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid())))
				.AddSingleton<IChannelFactory, ChannelFactory>() //TODO: Should this be one/application?
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<INamingConvetions, NamingConvetions>()
				.AddTransient<ISubscriber<MessageContext>, Subscriber<MessageContext>>()
				.AddTransient<IPublisher, Publisher<MessageContext>>()
				.AddTransient<IResponder<MessageContext>, Responder<MessageContext>>()
				.AddTransient<IRequester, Requester<MessageContext>>(
					p => new Requester<MessageContext>(
						p.GetService<IChannelFactory>(),
						p.GetService<IConsumerFactory>(),
						p.GetService<IMessageSerializer>(),
						p.GetService<IMessageContextProvider<MessageContext>>(),
						p.GetService<RawRabbitConfiguration>().RequestTimeout));
			configureIoc?.Invoke(services);
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
