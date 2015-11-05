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
using RawRabbit.Logging;

namespace RawRabbit.Consumer.Eventing
{
	public class EventingBasicConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ConcurrentBag<string> _processedButNotAcked;
		private readonly ConcurrentBag<IRawConsumer> _consumers;
		private readonly ILogger _logger = LogManager.GetLogger<EventingBasicConsumerFactory>();

		public EventingBasicConsumerFactory(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_processedButNotAcked = new ConcurrentBag<string>();
			_consumers = new ConcurrentBag<IRawConsumer>();
		}

		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg)
		{
			return CreateConsumer(cfg, _channelFactory.GetChannel());
		}

		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel)
		{
			ConfigureQos(channel, cfg.PrefetchCount);
			var rawConsumer = new EventingRawConsumer(channel);
			SetupLogging(rawConsumer);
			
			_consumers.Add(rawConsumer);
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
					_logger.LogInformation($"Message recived: MessageId: {args.BasicProperties.MessageId}");
					onMessageTask = rawConsumer.OnMessageAsync(sender, args);
				}
				catch (Exception)
				{
					/*
						The message handler threw an exception. It is time to hand over the
						message handling to an error strategy instead.
					*/
					if (!cfg.NoAck)
					{
						BasicAck(channel, args, cfg); // TODO: employ error handling strategy instead
					}
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

		private void SetupLogging(EventingRawConsumer rawConsumer)
		{
			rawConsumer.Shutdown += (sender, args) =>
			{
				_logger.LogInformation($"Consumer {rawConsumer.ConsumerTag} has been shut down.\n  Reason: {args.Cause}\n  Initiator: {args.Initiator}\n  Reply Text: {args.ReplyText}");
			};
			rawConsumer.ConsumerCancelled +=
				(sender, args) => _logger.LogDebug($"The consumer with tag '{args.ConsumerTag}' has been cancelled.");
			rawConsumer.Unregistered +=
				(sender, args) => _logger.LogDebug($"The consumer with tag '{args.ConsumerTag}' has been unregistered.");
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			_logger.LogDebug($"Setting QoS\n  Prefetch Size: 0\n  Prefetch Count: {prefetchCount}\n  global: false");
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
				_logger.LogDebug($"Ack:ing message with id {args.DeliveryTag}.");
				channel.BasicAck(
					deliveryTag: args.DeliveryTag,
					multiple: false
				);
			}
			catch (AlreadyClosedException)
			{
				_logger.LogWarning("Unable to ack message, channel is allready closed.");
				_processedButNotAcked.Add(args.BasicProperties.MessageId);
				CreateConsumer(cfg); // TODO: do we really want to re-conect? probably not.
			}
		}

		public void Dispose()
		{
			foreach (var consumer in _consumers)
			{
				consumer?.Disconnect();
			}
		}
	}
}