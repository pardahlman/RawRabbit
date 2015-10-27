using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Consumer.Eventing
{
	public class EventingBasicConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ConcurrentBag<string> _processedButNotAcked;

		public EventingBasicConsumerFactory(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_processedButNotAcked = new ConcurrentBag<string>();
		}

		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg)
		{
			var channel = _channelFactory.GetChannel();
			ConfigureQos(channel, cfg.PrefetchCount);
			var rawConsumer = new EventingRawConsumer(channel);
			channel.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, rawConsumer);

			rawConsumer.Received += (sender, args) =>
			{
				if (_processedButNotAcked.Contains(args.BasicProperties.MessageId))
				{
					/*
						This instance of the consumer has allready handled this message,
						but something went wrong when 'ack'-ing the message, therefore
						and the message was resent.
					*/
					BasicAck(channel, args, cfg);
					return;
				}

				Task onMessageTask;
				try
				{
					onMessageTask = rawConsumer.OnMessageAsync(sender, args);
				}
				catch (Exception)
				{
					/*
						The message handler threw an exception. It is time to hand over the
						message handling to an error strategy instead.
					*/
					BasicAck(channel, args, cfg); // TODO: employ error handling strategy instead
					return;
				}
				onMessageTask
					.ContinueWith(t =>
					{
						if (cfg.NoAck)
						{
							/*
								The consumer has stated that 'ack'-ing is not required, so
								now that the message is handled, the consumer is done.
							*/
							return;
						}
							
						BasicAck(channel, args, cfg);
					});
			};

			return rawConsumer;
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			channel.BasicQos(
				prefetchSize: 0,
				prefetchCount: prefetchCount,
				global: false
			);
		}

		protected void BasicAck(IModel channel, BasicDeliverEventArgs args, IConsumerConfiguration cfg)
		{
			try
			{
				channel.BasicAck(
					deliveryTag: args.DeliveryTag,
					multiple: false
				);
			}
			catch (AlreadyClosedException)
			{
				_processedButNotAcked.Add(args.BasicProperties.MessageId);
				CreateConsumer(cfg); // TODO: do we really want to re-conect? probably not.
			}
		}
	}
}