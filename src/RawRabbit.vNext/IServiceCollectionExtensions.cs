using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Contract;
using RawRabbit.Consumer.Eventing;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.vNext
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, IConfigurationRoot config = null, Action<IServiceCollection> custom = null)
		{
			collection
				.AddSingleton<RawRabbitConfiguration>(p =>
					config == null
						? new RawRabbitConfiguration()
						: p.GetService<IConfigurationParser>().Parse(config))
				.AddSingleton<IEnumerable<IConnectionFactory>>(p =>
				{
					var brokers = p.GetService<RawRabbitConfiguration>().Brokers ?? new List<BrokerConfiguration>();
					brokers = brokers.Any() ? brokers : new List<BrokerConfiguration> { BrokerConfiguration.Local };
					return brokers.Select(b => new ConnectionFactory
					{
						HostName = b.Hostname,
						VirtualHost = b.VirtualHost,
						UserName = b.Username,
						Password = b.Password
					});
				})
				.AddSingleton<IConnectionBroker, DefaultConnectionBroker>(
					p => new DefaultConnectionBroker(
						p.GetService<IEnumerable<IConnectionFactory>>(),
						TimeSpan.FromMinutes(1) //TODO: Move this to config
					)
				)
				.AddTransient<IConfigurationParser, ConfigurationParser>()
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
			custom?.Invoke(collection);
			return collection;
		}
	}
}
