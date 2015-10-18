using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common
{
	public interface IRawSubscriber
	{
		Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase;
	}

	public class RawSubscriber : IRawSubscriber
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IMessageSerializer _serializer;

		public RawSubscriber(IChannelFactory channelFactory, IMessageSerializer serializer)
		{
			_channelFactory = channelFactory;
			_serializer = serializer;
		}

		public Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase
		{
			var channel = _channelFactory.GetChannel();

			channel.QueueDeclare(
				queue: config.Queue.QueueName,
				durable: config.Queue.Durable,
				exclusive: config.Queue.Exclusive,
				autoDelete: config.Queue.AutoDelete,
				arguments: config.Queue.Arguments
			);
			
			channel.BasicQos(
				prefetchSize: 0, //TODO : what is this?
				prefetchCount: config.PrefetchCount,
				global: false // https://www.rabbitmq.com/consumer-prefetch.html
			);

			if (!config.Exchange.IsDefaultExchange())
			{
				channel.ExchangeDeclare(
					exchange: config.Exchange.ExchangeName,
					type: config.Exchange.ExchangeType
				);

				channel.QueueBind(
					queue: config.Queue.QueueName,
					exchange: config.Exchange.ExchangeName,
					routingKey: config.RoutingKey
				);
			}

			var consumer = new EventingBasicConsumer(channel);
			consumer.Received += (model, ea) =>
			{
				var message = _serializer.Deserialize<T>(ea.Body);
				subscribeMethod(message, null);

				channel.BasicAck(
					deliveryTag: ea.DeliveryTag,
					multiple: false)
				;
			};

			channel.BasicConsume(
				queue: config.Queue.QueueName,
				noAck: config.NoAck,
				consumer: consumer
			);

			return Task.FromResult(true);
		}
	}
}
