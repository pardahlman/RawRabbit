using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public interface ITopologyProvider
	{
		Task DeclareExchangeAsync(ExchangeDeclaration exchange);
		Task DeclareQueueAsync(QueueDeclaration queue);
		Task BindQueueAsync(string queue, string exchange, string routingKey);
		Task UnbindQueueAsync(string queue, string exchange, string routingKey);
		bool IsDeclared(ExchangeDeclaration exchange);
		bool IsDeclared(QueueDeclaration exchange);
	}

	public class TopologyProvider : ITopologyProvider, IDisposable
	{
		private readonly IChannelFactory _channelFactory;
		private IModel _channel;
		private readonly object _processLock = new object();
		private readonly Task _completed = Task.FromResult(true);
		private readonly Timer _disposeTimer;
		private readonly List<string> _initExchanges;
		private readonly List<string> _initQueues;
		private readonly List<string> _queueBinds;
		private readonly ConcurrentQueue<ScheduledTopologyTask> _topologyTasks;
		private readonly ILogger _logger = LogManager.GetLogger<TopologyProvider>();

		public TopologyProvider(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_initExchanges = new List<string>();
			_initQueues = new List<string>();
			_queueBinds = new List<string>();
			_topologyTasks = new ConcurrentQueue<ScheduledTopologyTask>();
			_disposeTimer = new Timer(state =>
			{
				_logger.LogInformation("Disposing topology channel (if exists).");
				_channel?.Dispose();
				_disposeTimer.Change(TimeSpan.FromHours(1), new TimeSpan(-1));
			}, null, TimeSpan.FromSeconds(2), new TimeSpan(-1));
		}

		public Task DeclareExchangeAsync(ExchangeDeclaration exchange)
		{
			if (IsDeclared(exchange))
			{
				return _completed;
			}

			var scheduled = new ScheduledExchangeTask(exchange);
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public Task DeclareQueueAsync(QueueDeclaration queue)
		{
			if (IsDeclared(queue))
			{
				return _completed;
			}

			var scheduled = new ScheduledQueueTask(queue);
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public Task BindQueueAsync(string queue, string exchange, string routingKey)
		{
			if (string.Equals(exchange, string.Empty))
			{
				/*
					"The default exchange is implicitly bound to every queue,
					with a routing key equal to the queue name. It it not possible
					to explicitly bind to, or unbind from the default exchange."
				*/
				return _completed;
			}

			var bindKey = $"{queue}_{exchange}_{routingKey}";
			if (_queueBinds.Contains(bindKey))
			{
				return _completed;
			}
			var scheduled = new ScheduledBindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey
			};
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public Task UnbindQueueAsync(string queue, string exchange, string routingKey)
		{
			var scheduled = new ScheduledUnbindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey
			};
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public bool IsDeclared(ExchangeDeclaration exchange)
		{
			return exchange.IsDefaultExchange() || exchange.AssumeInitialized || _initExchanges.Contains(exchange.ExchangeName);
		}

		public bool IsDeclared(QueueDeclaration queue)
		{
			return queue.IsDirectReplyTo() || _initQueues.Contains(queue.FullQueueName);
		}

		private void BindQueueToExchange(ScheduledBindQueueTask bind)
		{
			var bindKey = $"{bind.Queue}_{bind.Exchange}_{bind.RoutingKey}";
			if (_queueBinds.Contains(bindKey))
			{
				return;
			}

			_logger.LogInformation($"Binding queue '{bind.Queue}' to exchange '{bind.Exchange}' with routing key '{bind.RoutingKey}'");

			var channel = GetOrCreateChannel();
			channel.QueueBind(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey
				);
			_queueBinds.Add(bindKey);
		}

		private void UnbindQueueFromExchange(ScheduledUnbindQueueTask bind)
		{
			_logger.LogInformation($"Unbinding queue '{bind.Queue}' from exchange '{bind.Exchange}' with routing key '{bind.RoutingKey}'");

			var channel = GetOrCreateChannel();
			channel.QueueUnbind(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey,
				arguments: null
			);
			var bindKey = $"{bind.Queue}_{bind.Exchange}_{bind.RoutingKey}";
			if (_queueBinds.Contains(bindKey))
			{
				_queueBinds.Remove(bindKey);
			}
		}

		private void DeclareQueue(QueueDeclaration queue)
		{
			if (IsDeclared(queue))
			{
				return;
			}

			_logger.LogInformation($"Declaring queue '{queue.FullQueueName}'.");

			var channel = GetOrCreateChannel();
			channel.QueueDeclare(
				queue.FullQueueName,
				queue.Durable,
				queue.Exclusive,
				queue.AutoDelete,
				queue.Arguments);

			if (queue.AutoDelete)
			{
				_initQueues.Add(queue.FullQueueName);
			}
		}

		private void DeclareExchange(ExchangeDeclaration exchange)
		{
			if (IsDeclared(exchange))
			{
				return;
			}

			_logger.LogInformation($"Declaring exchange '{exchange.ExchangeName}'.");
			var channel = GetOrCreateChannel();
			channel.ExchangeDeclare(
				exchange.ExchangeName,
				exchange.ExchangeType,
				exchange.Durable,
				exchange.AutoDelete,
				exchange.Arguments);
			if (!exchange.AutoDelete)
			{
				_initExchanges.Add(exchange.ExchangeName);
			}
		}

		private void EnsureWorker()
		{
			if (!Monitor.TryEnter(_processLock))
			{
				return;
			}

			ScheduledTopologyTask topologyTask;
			while (_topologyTasks.TryDequeue(out topologyTask))
			{
				var exchange = topologyTask as ScheduledExchangeTask;
				if (exchange != null)
				{
					try
					{
						DeclareExchange(exchange.Declaration);
						exchange.TaskCompletionSource.TrySetResult(true);
					}
					catch (Exception e)
					{
						_logger.LogError($"Unable to declare exchange {exchange.Declaration.ExchangeName}", e);
						exchange.TaskCompletionSource.TrySetException(e);
					}

					continue;
				}

				var queue = topologyTask as ScheduledQueueTask;
				if (queue != null)
				{
					try
					{
						DeclareQueue(queue.Configuration);
						queue.TaskCompletionSource.TrySetResult(true);
					}
					catch (Exception e)
					{
						_logger.LogError($"Unable to declare queue", e);
						queue.TaskCompletionSource.TrySetException(e);
					}

					continue;
				}

				var bind = topologyTask as ScheduledBindQueueTask;
				if (bind != null)
				{
					try
					{
						BindQueueToExchange(bind);
						bind.TaskCompletionSource.TrySetResult(true);
					}
					catch (Exception e)
					{
						_logger.LogError($"Unable to bind queue", e);
						bind.TaskCompletionSource.TrySetException(e);
					}
					continue;
				}

				var unbind = topologyTask as ScheduledUnbindQueueTask;
				if (unbind != null)
				{
					try
					{
						UnbindQueueFromExchange(unbind);
						unbind.TaskCompletionSource.TrySetResult(true);
					}
					catch (Exception e)
					{
						_logger.LogError($"Unable to unbind queue", e);
						unbind.TaskCompletionSource.TrySetException(e);
					}
					
					continue;
				}
			}
			_logger.LogDebug($"Done processing topology work.");
			Monitor.Exit(_processLock);
		}

		private IModel GetOrCreateChannel()
		{
			_disposeTimer.Change(TimeSpan.FromSeconds(2), new TimeSpan(-1));
			if (_channel?.IsOpen ?? false)
			{
				return _channel;
			}

			var channelTask = _channelFactory.CreateChannelAsync();
			channelTask.Wait();
			_channel = channelTask.Result;
			return _channel;
		}

		public void Dispose()
		{
			_channelFactory?.Dispose();
		}

		#region Classes for Scheduled Tasks
		private abstract class ScheduledTopologyTask
		{
			protected ScheduledTopologyTask()
			{
				TaskCompletionSource = new TaskCompletionSource<bool>();
			}
			public TaskCompletionSource<bool> TaskCompletionSource { get; }
		}

		private class ScheduledQueueTask : ScheduledTopologyTask
		{
			public ScheduledQueueTask(QueueDeclaration queue)
			{
				Configuration = queue;
			}
			public QueueDeclaration Configuration { get; }
		}

		private class ScheduledExchangeTask : ScheduledTopologyTask
		{
			public ScheduledExchangeTask(ExchangeDeclaration exchange)
			{
				Declaration = exchange;
			}
			public ExchangeDeclaration Declaration { get; }
		}

		private class ScheduledBindQueueTask : ScheduledTopologyTask
		{
			public string Exchange { get; set; }
			public string Queue { get; set; }
			public string RoutingKey { get; set; }
		}

		private class ScheduledUnbindQueueTask : ScheduledTopologyTask
		{
			public string Exchange { get; set; }
			public string Queue { get; set; }
			public string RoutingKey { get; set; }
		}
		#endregion
	}
}
