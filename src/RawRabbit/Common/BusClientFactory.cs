using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Conventions;
using RawRabbit.Operations;
using RawRabbit.Serialization;

namespace RawRabbit.Common
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config = null)
		{
			config = config ?? RawRabbitConfiguration.Default;
			var connection = new ConnectionFactory {HostName = config.Hostname}.CreateConnection();
			var channelFactory = new ChannelFactory(connection);
			var contextProvider = new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid()));
			var serializer = new JsonMessageSerializer();
			return new BusClient(
				new ConfigurationEvaluator(new QueueConventions(), new ExchangeConventions()),
				new Subscriber<MessageContext>(channelFactory, serializer, contextProvider),
				new Publisher<MessageContext>(channelFactory, serializer, contextProvider),
				new Responder<MessageContext>(channelFactory, serializer, contextProvider),
				new Requester<MessageContext>(channelFactory, serializer, contextProvider)
			);
		}
	}
}
