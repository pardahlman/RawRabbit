using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Common
{
	public interface ITopologyProvider
	{
		Task DeclareExchangeAsync(ExchangeConfiguration exchange);
		Task DeclareQueueAsync(QueueConfiguration queue);
		Task BindQueueAsync(QueueConfiguration queue, ExchangeConfiguration exchange, string routingKey);
	}

	public class TopologyProvider : ITopologyProvider, IDisposable
	{
		private readonly IChannelFactory _channelFactory;
		private readonly Task _completed = Task.FromResult(true);
		private readonly ConcurrentDictionary<string, Task> _initExchanges; 
		private readonly ConcurrentDictionary<string, Task> _initQueues; 

		public TopologyProvider(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_initExchanges = new ConcurrentDictionary<string, Task>();
			_initQueues = new ConcurrentDictionary<string, Task>();
		}

		public Task DeclareExchangeAsync(ExchangeConfiguration exchange)
		{
			Task existingTask;
			if (_initExchanges.TryGetValue(exchange.ExchangeName, out existingTask))
			{
				return existingTask;
			}
			
			if (exchange.IsDefaultExchange() || exchange.AssumeInitialized)
			{
				_initExchanges.TryAdd(exchange.ExchangeName, _completed);
				return _completed;
			}

			var exchangeTask = _channelFactory
					.GetChannelAsync()
					.ContinueWith(tChannel =>
					{
						tChannel.Result.ExchangeDeclare(
							exchange.ExchangeName,
							exchange.ExchangeType,
							exchange.Durable,
							exchange.AutoDelete,
							exchange.Arguments);
					});
			_initExchanges.TryAdd(exchange.ExchangeName, exchangeTask);
			return exchangeTask;
		}

		public Task DeclareQueueAsync(QueueConfiguration queue)
		{
			Task existingTask;
			if (_initQueues.TryGetValue(queue.FullQueueName, out existingTask))
			{
				return existingTask;
			}
			

			if (queue.IsDirectReplyTo())
			{
				/*
					"Consume from the pseudo-queue amq.rabbitmq.reply-to in no-ack mode. There is no need to
					declare this "queue" first, although the client can do so if it wants."
					- https://www.rabbitmq.com/direct-reply-to.html
				*/
				_initQueues.TryAdd(queue.FullQueueName, _completed);
				return _completed;
			}

			var queueTask = _channelFactory
				.GetChannelAsync()
				.ContinueWith(tChannel =>
				{
					tChannel.Result.QueueDeclare(
						queue.FullQueueName,
						queue.Durable,
						queue.Exclusive,
						queue.AutoDelete,
						queue.Arguments);
				});
			_initQueues.TryAdd(queue.FullQueueName, queueTask);
			return queueTask;
		}

		public Task BindQueueAsync(QueueConfiguration queue, ExchangeConfiguration exchange, string routingKey)
		{
			var queueTask = DeclareQueueAsync(queue);
			var exchangeTask = DeclareExchangeAsync(exchange);
			var channelTask = _channelFactory.CreateChannelAsync();

			return Task
				.WhenAll(queueTask, exchangeTask, channelTask)
				.ContinueWith(t =>
				{
					channelTask.Result
						.QueueBind(
							queue: queue.FullQueueName,
							exchange: exchange.ExchangeName,
							routingKey: routingKey
						);
				});
		}

		public void Dispose()
		{
			_channelFactory?.Dispose();
		}
	}
}
