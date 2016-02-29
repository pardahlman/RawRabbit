using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;

namespace RawRabbit.Consumer.Eventing
{
	public class EventingBasicConsumerFactory : IConsumerFactory, IDisposable
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IErrorHandlingStrategy _strategy;
		private readonly ConcurrentBag<string> _processedButNotAcked;
		private readonly ConcurrentBag<IRawConsumer> _consumers;
		private readonly ILogger _logger = LogManager.GetLogger<EventingBasicConsumerFactory>();

		public EventingBasicConsumerFactory(IChannelFactory channelFactory, IErrorHandlingStrategy strategy)
		{
			_channelFactory = channelFactory;
			_strategy = strategy;
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
			
			_consumers.Add(rawConsumer);

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
					try
					{
						rawConsumer
							.OnMessageAsync(sender, args)
							.ContinueWith(t =>
							{
								if (t.IsFaulted)
								{
									_logger.LogError($"An unhandled exception was caught for message {args.BasicProperties.MessageId}.\n", t.Exception);
									_strategy.OnRequestHandlerExceptionAsync(rawConsumer, cfg, args, t.Exception);
									return;
								}
								if (cfg.NoAck || rawConsumer.NackedDeliveryTags.Contains(args.DeliveryTag))
								{
								/*
									The consumer has stated that 'ack'-ing is not required, so
									now that the message is handled, the consumer is done.
								*/
									return;
								}

								BasicAck(channel, args);
						});
					}
					catch (Exception e)
					{
						_logger.LogError($"An unhandled exception was caught for message {args.BasicProperties.MessageId}.\n", e);
						_strategy.OnRequestHandlerExceptionAsync(rawConsumer, cfg, args, e);
					}
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

		public void Dispose()
		{
			_channelFactory?.Dispose();
			foreach (var consumer in _consumers)
			{
				consumer?.Disconnect();
			}
		}
	}
}