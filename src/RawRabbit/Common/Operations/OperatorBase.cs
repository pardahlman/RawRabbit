using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Common.Operations
{
	public abstract class OperatorBase : IDisposable
	{
		protected readonly IChannelFactory ChannelFactory;

		protected OperatorBase(IChannelFactory channelFactory)
		{
			ChannelFactory = channelFactory;
		}
		
		protected Task DeclareExchangeAsync(ExchangeConfiguration config)
		{
			if (config.IsDefaultExchange() || config.AssumeInitialized)
			{
				return Task.FromResult(true);
			}
			return Task.Factory.StartNew(() =>
				ChannelFactory
					.GetChannel()
					.ExchangeDeclare(
						exchange: config.ExchangeName,
						type: config.ExchangeType
					)
				);
		}

		protected Task DeclareQueueAsync(QueueConfiguration config)
		{
			return Task.Factory.StartNew(() =>
				ChannelFactory
					.GetChannel()
					.QueueDeclare(
						queue: config.QueueName,
						durable: config.Durable,
						exclusive: config.Exclusive,
						autoDelete: config.AutoDelete,
						arguments: config.Arguments
					)
				);
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

		protected void ConfigureQosAsync(IModel channel, ushort prefetchCount)
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
