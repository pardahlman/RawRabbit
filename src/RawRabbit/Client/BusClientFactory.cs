using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Common.Conventions;
using RawRabbit.Common.Serialization;

namespace RawRabbit.Client
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config = null)
		{
			config = config ?? RawRabbitConfiguration.Default;
			var connection = new ConnectionFactory {HostName = config.Hostname}.CreateConnection();
			var channelFactory = new ChannelFactory(connection);
			var serializer = new JsonMessageSerializer();
			return new BusClient(
				new ConfigurationEvaluator(new QueueConventions(), new ExchangeConventions()),
				new RawSubscriber(channelFactory, serializer),
				new RawPublisher(channelFactory, serializer)
			);
		}
	}
}
