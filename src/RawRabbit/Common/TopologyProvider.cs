using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
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
		Task BindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments);
		Task UnbindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments);
		bool IsDeclared(ExchangeDeclaration exchange);
		bool IsDeclared(QueueDeclaration exchange);
	}

	public class TopologyProvider : ITopologyProvider, IDisposable
	{
		private readonly IChannelFactory _channelFactory;
		private IModel _channel;
		private readonly object _processLock = new object();
		private readonly Task _completed = Task.FromResult(true);
		private readonly List<string> _initExchanges;
		private readonly List<string> _initQueues;
		private readonly List<string> _queueBinds;
		private readonly ConcurrentQueue<ScheduledTopologyTask> _topologyTasks;
		private readonly ILog _logger = LogProvider.For<TopologyProvider>();

		public TopologyProvider(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
			_initExchanges = new List<string>();
			_initQueues = new List<string>();
			_queueBinds = new List<string>();
			_topologyTasks = new ConcurrentQueue<ScheduledTopologyTask>();
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

		public Task BindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string,object> arguments)
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

			var bindKey = CreateBindKey(queue, exchange, routingKey, arguments);
			if (_queueBinds.Contains(bindKey))
			{
				return _completed;
			}
			var scheduled = new ScheduledBindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey,
				Arguments = arguments
			};
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public Task UnbindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
		{
			var scheduled = new ScheduledUnbindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey,
				Arguments = arguments
			};
			_topologyTasks.Enqueue(scheduled);
			EnsureWorker();
			return scheduled.TaskCompletionSource.Task;
		}

		public bool IsDeclared(ExchangeDeclaration exchange)
		{
			return exchange.IsDefaultExchange() || _initExchanges.Contains(exchange.Name);
		}

		public bool IsDeclared(QueueDeclaration queue)
		{
			return queue.IsDirectReplyTo() || _initQueues.Contains(queue.Name);
		}

		private void BindQueueToExchange(ScheduledBindQueueTask bind)
		{
			string bindKey = CreateBindKey(bind);
			if (_queueBinds.Contains(bindKey))
			{
				return;
			}

			_logger.Info("Binding queue {queueName} to exchange {exchangeName} with routing key {routingKey}", bind.Queue, bind.Exchange, bind.RoutingKey);

			var channel = GetOrCreateChannel();
			channel.QueueBind(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey,
				arguments: bind.Arguments
			);
			_queueBinds.Add(bindKey);
		}

		private void UnbindQueueFromExchange(ScheduledUnbindQueueTask bind)
		{
			_logger.Info("Unbinding queue {queueName} from exchange {exchangeName} with routing key {routingKey}", bind.Queue, bind.Exchange, bind.RoutingKey);

			var channel = GetOrCreateChannel();
			channel.QueueUnbind(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey,
				arguments: bind.Arguments
			);
			var bindKey = CreateBindKey(bind);
			if (_queueBinds.Contains(bindKey))
			{
				_queueBinds.Remove(bindKey);
			}
		}
		
		private static string CreateBindKey(ScheduledBindQueueTask bind)
		{
			return CreateBindKey(bind.Queue, bind.Exchange, bind.RoutingKey, bind.Arguments);
		}

		private static string CreateBindKey(ScheduledUnbindQueueTask bind)
		{
			return CreateBindKey(bind.Queue, bind.Exchange, bind.RoutingKey, bind.Arguments);
		}

		private static string CreateBindKey(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
		{
			var bindKey = $"{queue}_{exchange}_{routingKey}";
			if (arguments != null && arguments.Count > 0)
			{
				// order the arguments, for the key to be identical no matter the ordering
				IOrderedEnumerable<KeyValuePair<string, object>> orderedArguments = arguments.OrderBy(pair => pair.Key);
				bindKey = $"{bindKey}_{string.Join("_", orderedArguments.Select(pair => $"{pair.Key}:{pair.Value}"))}";
			}
			return bindKey;
		}

		private void DeclareQueue(QueueDeclaration queue)
		{
			if (IsDeclared(queue))
			{
				return;
			}

			_logger.Info("Declaring queue {queueName}.", queue.Name);

			var channel = GetOrCreateChannel();
			channel.QueueDeclare(
				queue.Name,
				queue.Durable,
				queue.Exclusive,
				queue.AutoDelete,
				queue.Arguments);

			if (queue.AutoDelete)
			{
				_initQueues.Add(queue.Name);
			}
		}

		private void DeclareExchange(ExchangeDeclaration exchange)
		{
			if (IsDeclared(exchange))
			{
				return;
			}

			_logger.Info("Declaring exchange {exchangeName}.", exchange.Name);
			var channel = GetOrCreateChannel();
			channel.ExchangeDeclare(
				exchange.Name,
				exchange.ExchangeType,
				exchange.Durable,
				exchange.AutoDelete,
				exchange.Arguments);
			if (!exchange.AutoDelete)
			{
				_initExchanges.Add(exchange.Name);
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
						_logger.Error(e, "Unable to declare exchange {exchangeName}", exchange.Declaration.Name);
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
						_logger.Error(e, "Unable to declare queue");
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
						_logger.Error(e, "Unable to bind queue");
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
						_logger.Error(e, "Unable to unbind queue");
						unbind.TaskCompletionSource.TrySetException(e);
					}
				}
			}
			_logger.Debug("Done processing topology work.");
			Monitor.Exit(_processLock);
		}

		private IModel GetOrCreateChannel()
		{
			if (_channel?.IsOpen ?? false)
			{
				return _channel;
			}

			_channel = _channelFactory
				.CreateChannelAsync()
				.GetAwaiter()
				.GetResult();
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
			public IDictionary<string,object> Arguments { get; set; }
		}

		private class ScheduledUnbindQueueTask : ScheduledTopologyTask
		{
			public string Exchange { get; set; }
			public string Queue { get; set; }
			public string RoutingKey { get; set; }
			public IDictionary<string, object> Arguments { get; set; }
		}
		#endregion
	}
}
