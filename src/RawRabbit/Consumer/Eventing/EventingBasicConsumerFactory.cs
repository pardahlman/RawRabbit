using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Consumer.Eventing
{
	public class EventingBasicConsumerFactory : IRawConsumerFactory
	{
		private readonly ConcurrentBag<string> _processedButNotAcked;
		private readonly ILogger _logger = LogManager.GetLogger<EventingBasicConsumerFactory>();

		public EventingBasicConsumerFactory()
		{
			_processedButNotAcked = new ConcurrentBag<string>();
		}

		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel)
		{
			return CreateConsumer(cfg.PrefetchCount, cfg.NoAck, channel);
		}

		public IRawConsumer CreateConsumer(ConsumeConfiguration cfg, IModel channel)
		{
			return CreateConsumer(cfg.PrefetchCount, cfg.NoAck, channel);
		}

		protected virtual IRawConsumer CreateConsumer(ushort prefetch, bool noAck, IModel channel)
		{
			ConfigureQos(channel, prefetch);
			var rawConsumer = new EventingRawConsumer(channel);

			rawConsumer.Received += (sender, args) =>
			{
				Task.Run(() =>
				{
					if (_processedButNotAcked.Contains(args.BasicProperties.MessageId))
					{
						/*
							This instance of the consumer has allready handled this message,
							but something went wrong when 'ack'-ing the message, therefore
							and the message was resent.
						*/
						BasicAck(channel, args);
						return;
					}
					_logger.LogInformation($"Message recived: MessageId: {args.BasicProperties.MessageId}");
					rawConsumer
						.OnMessageAsync(sender, args)
						.ContinueWith(t =>
						{
							if (noAck || rawConsumer.AcknowledgedTags.Contains(args.DeliveryTag))
							{
								return;
							}
							BasicAck(channel, args);
						});
				});
			};

			return rawConsumer;
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

		protected void BasicAck(IModel channel, BasicDeliverEventArgs args)
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
			}
		}
	}
}