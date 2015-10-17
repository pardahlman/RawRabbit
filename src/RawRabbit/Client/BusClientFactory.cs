using RabbitMQ.Client;
using RawRabbit.Common;

namespace RawRabbit.Client
{
	public class BusClientFactory
	{
		public static BusClient CreateDefault(RawRabbitConfiguration config = null)
		{
			config = config ?? RawRabbitConfiguration.Default;
			var connection = new ConnectionFactory {HostName = config.Hostname}.CreateConnection();
			var channelFactory = new ChannelFactory(connection);
			
			return new BusClient(
				new ConfigurationEvaluator(),
				new RawSubscriber(channelFactory),
				new RawPublisher(channelFactory)
			);
		}
	}
}
