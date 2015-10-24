using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations;
using RawRabbit.Serialization;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config = null)
		{
			config = config ?? RawRabbitConfiguration.Default;
			var connection = CreateConnection(config);
			var channelFactory = new ChannelFactory(connection);
			var contextProvider = new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid()));
			var serializer = new JsonMessageSerializer();
			return new BusClient(
				new ConfigurationEvaluator(config, new NamingConvetions()),
				new Subscriber<MessageContext>(channelFactory, serializer, contextProvider),
				new Publisher<MessageContext>(channelFactory, serializer, contextProvider),
				new Responder<MessageContext>(channelFactory, serializer, contextProvider),
				new Requester<MessageContext>(channelFactory, serializer, contextProvider, config.RequestTimeout)
			);
		}

		private static IConnection CreateConnection(RawRabbitConfiguration config)
		{
			var factory = new ConnectionFactory
			{
				HostName = config.Hostname,
				UserName = config.Username,
				Password = config.Password
			};
			try
			{
				return factory.CreateConnection();
			}
			catch (BrokerUnreachableException e)
			{
				if (e.InnerException is AuthenticationFailureException)
				{
					throw e.InnerException;
				}
				throw;
			}
		}

		public static BusClient CreateDefault(TimeSpan requestTimeout)
		{
			var config = new RawRabbitConfiguration
			{
				RequestTimeout = requestTimeout
			};
			var connection = new ConnectionFactory { HostName = config.Hostname }.CreateConnection();
			var channelFactory = new ChannelFactory(connection);
			var contextProvider = new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid()));
			var serializer = new JsonMessageSerializer();
			return new BusClient(
				new ConfigurationEvaluator(config, new NamingConvetions()),
				new Subscriber<MessageContext>(channelFactory, serializer, contextProvider),
				new Publisher<MessageContext>(channelFactory, serializer, contextProvider),
				new Responder<MessageContext>(channelFactory, serializer, contextProvider),
				new Requester<MessageContext>(channelFactory, serializer, contextProvider, config.RequestTimeout)
			);
		}
	}
}
