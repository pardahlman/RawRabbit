using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Consumer
{
	public interface IConsumerFactory
	{
		Task<IBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg);
		Task<IBasicConsumer> CreateConsumerAsync(ConsumeConfiguration cfg);
	}

	public class ConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>> _noAckConsumers;
		private readonly ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>> _ackConsumers;

		public ConsumerFactory(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_noAckConsumers = new ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>>();
			_ackConsumers = new ConcurrentDictionary<string, Lazy<Task<IBasicConsumer>>>();
		}

		public Task<IBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg)
		{
			var cache = cfg.NoAck ? _noAckConsumers : _ackConsumers;
			var lazyConsumerTask = cache.GetOrAdd(cfg.RoutingKey, routingKey =>
			{
				return new Lazy<Task<IBasicConsumer>>(() => CreateConsumerAsync(cfg));
			});
			return lazyConsumerTask.Value;
		}

		public Task<IBasicConsumer> CreateConsumerAsync(ConsumeConfiguration cfg)
		{
			return GetOrCreateChannelAsync()
				.ContinueWith(tChannel =>
				{
					IBasicConsumer consumer = new EventingBasicConsumer(tChannel.Result);
					tChannel.Result.BasicConsume(
						queue: cfg.QueueName,
						noAck: cfg.NoAck,
						consumerTag: cfg.ConsumerTag,
						noLocal: cfg.NoLocal,
						exclusive: cfg.Exclusive,
						arguments: cfg.Arguments,
						consumer: consumer);
					return consumer;
				});
		}

		protected virtual Task<IModel> GetOrCreateChannelAsync()
		{
			return _channelFactory.CreateChannelAsync();
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
