using System;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations;
using RawRabbit.Serialization;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config = null, Action<IServiceCollection> configureIoc = null)
		{
			var services = new ServiceCollection();
			services
				.AddSingleton<RawRabbitConfiguration>(provider => config ?? new RawRabbitConfiguration())
				.AddTransient<IMessageSerializer, JsonMessageSerializer>()
				.AddSingleton<IMessageContextProvider<MessageContext>, DefaultMessageContextProvider>(
					p => new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid())))
				.AddTransient<IChannelFactory, ConfigChannelFactory>()
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<INamingConvetions, NamingConvetions>()
				.AddTransient<ISubscriber<MessageContext>, Subscriber<MessageContext>>()
				.AddTransient<IPublisher, Publisher<MessageContext>>()
				.AddTransient<IResponder<MessageContext>, Responder<MessageContext>>()
				.AddTransient<IRequester, Requester<MessageContext>>(
					p => new Requester<MessageContext>(
						p.GetService<IChannelFactory>(),
						p.GetService<IMessageSerializer>(),
						p.GetService<IMessageContextProvider<MessageContext>>(),
						p.GetService<RawRabbitConfiguration>().RequestTimeout));
			configureIoc?.Invoke(services);
			var serviceProvider = services.BuildServiceProvider();
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
			return CreateDefault(new RawRabbitConfiguration {RequestTimeout = requestTimeout});
		}
	}
}
