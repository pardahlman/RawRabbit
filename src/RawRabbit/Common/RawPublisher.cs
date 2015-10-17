using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RawRabbit.Core.Configuration.Publish;

namespace RawRabbit.Common
{
	public interface IRawPublisher
	{
		Task PublishAsync<T>(T message, PublishConfiguration config);
	}

	public class RawPublisher : IRawPublisher
	{
		private readonly IChannelFactory _channelFactory;

		public RawPublisher(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public Task PublishAsync<T>(T message, PublishConfiguration config)
		{
			var channel = _channelFactory.GetChannel();

			channel.ExchangeDeclare(
				exchange: config.Exchange.ExchangeName,
				type: config.Exchange.ExchangeType
			);

			var msgStr = JsonConvert.SerializeObject(message);
			var msgBytes = Encoding.UTF8.GetBytes(msgStr);
			channel.BasicPublish(
				exchange: config.Exchange.ExchangeName,
				routingKey: config.RoutingKey,
				basicProperties: null,
				body: msgBytes
			);
			return Task.FromResult(true);
		}
	}
}
