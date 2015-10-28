using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public abstract class OperatorBase : IDisposable
	{
		protected readonly IChannelFactory ChannelFactory;
		protected readonly IMessageSerializer Serializer;

		protected OperatorBase(IChannelFactory channelFactory, IMessageSerializer serializer)
		{
			ChannelFactory = channelFactory;
			Serializer = serializer;
		}
		
		protected Task DeclareExchangeAsync(ExchangeConfiguration config)
		{
			if (config.IsDefaultExchange() || config.AssumeInitialized)
			{
				return Task.FromResult(true);
			}
			return Task.Run(() =>
				ChannelFactory
					.GetChannel()
					.ExchangeDeclare(
						exchange: config.ExchangeName,
						type: config.ExchangeType
					)
				);
		}

		protected Task DeclareQueueAsync(QueueConfiguration queue)
		{
			if (queue.IsDirectReplyTo())
			{
				/*
					"Consume from the pseudo-queue amq.rabbitmq.reply-to in no-ack mode. There is no need to
					declare this "queue" first, although the client can do so if it wants."
					- https://www.rabbitmq.com/direct-reply-to.html
				*/
				return Task.FromResult(true);
			}
			return Task.Run(() =>
				ChannelFactory
					.GetChannel()
					.QueueDeclare(
						queue: queue.QueueName,
						durable: queue.Durable,
						exclusive: queue.Exclusive,
						autoDelete: queue.AutoDelete,
						arguments: queue.Arguments
					)
				);
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
			ChannelFactory
				.GetChannel()
				.QueueBind(
					queue: queue.QueueName,
					exchange: exchange.ExchangeName,
					routingKey: routingKey
				);
		}

		protected Task<byte[]> CreateMessageAsync<T>(T message)
		{
			return Task.Run(() => Serializer.Serialize(message));
		}

		protected void BasicAck(IModel channel, ulong deliveryTag)
		{
			/*
				Acknowledgement needs to be called on the same channel that
				delivered the message. This is the reason we're not using 
				the ChannelFactory in this instance.
			*/
			channel.BasicAck(
					deliveryTag: deliveryTag,
					multiple: false
				);
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			/*
				QoS is per consumer on channel. If ChannelFactory is used,
				we might get a new channel than the one the consumer is
				we are configuring.
			*/
			channel.BasicQos(
					prefetchSize: 0, //TODO : what is this?
					prefetchCount: prefetchCount,
					global: false // https://www.rabbitmq.com/consumer-prefetch.html
				);
		}

		public virtual void Dispose()
		{
			ChannelFactory?.Dispose();
		}
	}
}
