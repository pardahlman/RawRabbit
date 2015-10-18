using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Exchange;
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
		private readonly IMessageSerializer _serializer;

		public RawPublisher(IChannelFactory channelFactory, IMessageSerializer serializer)
		{
			_channelFactory = channelFactory;
			_serializer = serializer;
		}

		public Task PublishAsync<T>(T message, PublishConfiguration config)
		{
			var channel = _channelFactory.GetChannel();

			channel.QueueDeclare(
				queue:config.Queue.QueueName,
				durable:config.Queue.Durable,
				exclusive:config.Queue.Exclusive,
				autoDelete:config.Queue.AutoDelete,
				arguments: config.Queue.Arguments
			);

			if (!config.Exchange.IsDefaultExchange())
			{
				channel.ExchangeDeclare(
					exchange: config.Exchange.ExchangeName,
					type: config.Exchange.ExchangeType
				);
			}

			channel.BasicPublish(
				exchange: config.Exchange.ExchangeName,
				routingKey: config.RoutingKey,
				basicProperties: channel.CreateBasicProperties(), //TODO: move this to config
				body: _serializer.Serialize(message)
			);

			return Task.FromResult(true);
		}
	}
}
