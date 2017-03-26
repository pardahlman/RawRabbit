using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Consume;
using RawRabbit.Logging;

namespace RawRabbit.Consumer
{
	public class ConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;
		private static readonly ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>> NoAckConsumers = new ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>>();
		private static readonly ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>> AckConsumers = new ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>>();
		private readonly ILogger _logger = LogManager.GetLogger<ConsumerFactory>();

		public ConsumerFactory(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public Task<IBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg, IModel channel = null, CancellationToken token = default(CancellationToken))
		{
			var cache = cfg.NoAck ? NoAckConsumers : AckConsumers;
			var lazyConsumerTask = cache.GetOrAdd(cfg.RoutingKey, routingKey =>
			{
				return new Lazy<Task<IBasicConsumer>>(async () =>
				{
					var consumer = await CreateConsumerAsync(channel, token);
					ConfigureConsume(consumer, cfg);
					return consumer;
				});
			});
			return lazyConsumerTask.Value;
		}

		public async Task<IBasicConsumer> CreateConsumerAsync(IModel channel = null, CancellationToken token = default(CancellationToken))
		{
			if (channel == null)
			{
				channel = await GetOrCreateChannelAsync(token);
			}
			return new EventingBasicConsumer(channel);
		}

		public IBasicConsumer ConfigureConsume(IBasicConsumer consumer, ConsumeConfiguration cfg)
		{
			CheckPropertyValues(cfg);
			_logger.LogInformation($"Preparing to consume message from queue '{cfg.QueueName}'.");

			consumer.Model.BasicConsume(
				queue: cfg.QueueName,
				noAck: cfg.NoAck,
				consumerTag: cfg.ConsumerTag,
				noLocal: cfg.NoLocal,
				exclusive: cfg.Exclusive,
				arguments: cfg.Arguments,
				consumer: consumer);

			if (cfg.PrefetchCount > 0)
			{
				_logger.LogInformation($"Setting Prefetch Count to {cfg.PrefetchCount}.");
				consumer.Model.BasicQos(
					prefetchSize: 0,
					prefetchCount: cfg.PrefetchCount,
					global: false
				);
			}
			return consumer;
		}

		protected virtual void CheckPropertyValues(ConsumeConfiguration cfg)
		{
			if (cfg == null)
			{
				throw new ArgumentException("Unable to create consumer. The provided configuration is null");
			}
			if (string.IsNullOrEmpty(cfg.QueueName))
			{
				throw new ArgumentException("Unable to create consume. No queue name provided.");
			}
			if (string.IsNullOrEmpty(cfg.ConsumerTag))
			{
				throw new ArgumentException("Unable to create consume. Consumer tag cannot be undefined.");
			}
		}

		protected virtual Task<IModel> GetOrCreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			_logger.LogInformation("Creating a dedicated channel for consumer.");
			return _channelFactory.CreateChannelAsync(token);
		}
	}

	public static class ConsumerExtensions
	{
		public static Task<string> CancelAsync(this IBasicConsumer consumer, CancellationToken token = default(CancellationToken))
		{
			var eventConsumer = consumer as EventingBasicConsumer;
			if (eventConsumer == null)
			{
				throw new NotSupportedException("Can only cancellation EventBasicConsumer");
			}
			var cancelTcs = new TaskCompletionSource<string>();
			token.Register(() => cancelTcs.TrySetCanceled());
			var tag = eventConsumer.ConsumerTag;
			consumer.ConsumerCancelled += (sender, args) =>
			{
				if (args.ConsumerTag != tag)
				{
					return;
				}
				cancelTcs.TrySetResult(args.ConsumerTag);
			};
			consumer.Model.BasicCancel(eventConsumer.ConsumerTag);
			return cancelTcs.Task;
		}

		public static void OnMessage(this IBasicConsumer consumer, EventHandler<BasicDeliverEventArgs> onMessage, Predicate<BasicDeliverEventArgs> abort = null)
		{
			var eventConsumer = consumer as EventingBasicConsumer;
			if (eventConsumer == null)
			{
				throw new NotSupportedException("Only supported for EventBasicConsumer");
			}
			eventConsumer.Received += onMessage;

			if (abort == null)
			{
				return;
			}

			EventHandler<BasicDeliverEventArgs> abortHandler = null;
			abortHandler = (sender, args) =>
			{
				if (abort(args))
				{
					eventConsumer.Received -= onMessage;
					eventConsumer.Received -= abortHandler;
				}
			};
			eventConsumer.Received += abortHandler;
		}
	}
}
