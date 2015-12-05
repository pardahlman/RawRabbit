using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public abstract class OperatorBase : IDisposable
	{
		protected readonly IChannelFactory ChannelFactory;
		protected readonly IMessageSerializer Serializer;
		private readonly ILogger _logger = LogManager.GetLogger<OperatorBase>();

		protected OperatorBase(IChannelFactory channelFactory, IMessageSerializer serializer)
		{
			ChannelFactory = channelFactory;
			Serializer = serializer;
		}

		protected void DeclareExchange(ExchangeConfiguration config, IModel channel = null)
		{
			if (config.IsDefaultExchange() || config.AssumeInitialized)
			{
				return;
			}
			_logger.LogDebug($"Declaring exchange\n  Name: {config.ExchangeName}\n  Type: {config.ExchangeType}\n  Durable: {config.Durable}\n  Autodelete: {config.AutoDelete}");
			channel = channel ?? ChannelFactory.GetChannel();
			channel
				.ExchangeDeclare(
					exchange: config.ExchangeName,
					type: config.ExchangeType
				);
		}

		protected void DeclareQueue(QueueConfiguration queue, IModel channel = null)
		{
			if (queue.IsDirectReplyTo())
			{
				/*
					"Consume from the pseudo-queue amq.rabbitmq.reply-to in no-ack mode. There is no need to
					declare this "queue" first, although the client can do so if it wants."
					- https://www.rabbitmq.com/direct-reply-to.html
				*/
				return;
			}
			channel = channel ?? ChannelFactory.GetChannel();
			channel
				.QueueDeclare(
					queue: queue.FullQueueName,
					durable: queue.Durable,
					exclusive: queue.Exclusive,
					autoDelete: queue.AutoDelete,
					arguments: queue.Arguments
				);
			_logger.LogDebug($"Declaring queue\n  Name: {queue.FullQueueName}\n  Exclusive: {queue.Exclusive}\n  Durable: {queue.Durable}\n  Autodelete: {queue.AutoDelete}");
		}

		protected void BindQueue(QueueConfiguration queue, ExchangeConfiguration exchange, string routingKey)
		{
			if (exchange.IsDefaultExchange())
			{
				/*
					"The default exchange is implicitly bound to every queue,
					with a routing key equal to the queue name. It it not possible
					to explicitly bind to, or unbind from the default exchange."
				*/
				return;
			}
			if (queue.IsDirectReplyTo())
			{
				/*
					"Consume from the pseudo-queue amq.rabbitmq.reply-to in no-ack mode. There is no need to
					declare this "queue" first, although the client can do so if it wants."
					- https://www.rabbitmq.com/direct-reply-to.html
				*/
				return;
			}
			_logger.LogDebug($"Binding queue {queue.QueueName} to exchange {exchange.ExchangeName} with routing key {routingKey}");
			ChannelFactory
				.GetChannel()
				.QueueBind(
					queue: queue.FullQueueName,
					exchange: exchange.ExchangeName,
					routingKey: routingKey
				);
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			/*
				QoS is per consumer on channel. If ChannelFactory is used,
				we might get a new channel than the one the consumer is
				we are configuring.
			*/
			_logger.LogDebug($"Setting QoS\n  Prefetch Size: 0\n  Prefetch Count: {prefetchCount}\n  global: false");
			channel.BasicQos(
					prefetchSize: 0, //TODO : what is this?
					prefetchCount: prefetchCount,
					global: false // https://www.rabbitmq.com/consumer-prefetch.html
				);
		}

		public virtual void Dispose()
		{
			_logger.LogDebug($"Disposing operation.");
			ChannelFactory?.Dispose();
		}
	}
}
