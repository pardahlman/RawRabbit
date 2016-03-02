using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public class DynamicChannelFactory : IChannelFactory
	{
		private readonly IConnectionFactory _connectionFactory;
		private IConnection _connection;
		private readonly RawRabbitConfiguration _config;
		private readonly LinkedList<IModel> _channels;
		private LinkedListNode<IModel> _current;
		private readonly int _maxChannels;
		private readonly ILogger _logger = LogManager.GetLogger<ChannelFactory>();
		private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _requestQueue;
		private readonly object _channelLock = new object();

		public DynamicChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config)
		{
			_connection = connectionFactory.CreateConnection(config.Hostnames);
			_connectionFactory = connectionFactory;
			_config = config;
			_maxChannels = 5;
			_requestQueue = new ConcurrentQueue<TaskCompletionSource<IModel>>();
			_channels = new LinkedList<IModel>();
			var newChannelTask = CreateAndWireupAsync();
			newChannelTask.Wait();
			_channels.AddFirst(newChannelTask.Result);
			_current = _channels.First;
		}

		public void Dispose()
		{
			foreach (var channel in _channels)
			{
				channel?.Dispose();
			}
			_connection?.Dispose();
		}

		public IModel GetChannel()
		{
			return GetChannelAsync().Result;
		}

		public IModel CreateChannel(IConnection connection = null)
		{
			return CreateChannelAsync(connection).Result;
		}

		public Task<IModel> GetChannelAsync()
		{
			lock (_channelLock)
			{
				_current = _current.Next ?? _channels.First;

				if (_current.Value.IsOpen)
				{
					return Task.FromResult(_current.Value);
				}

				if (_current.Value.CloseReason.Initiator == ShutdownInitiator.Application)
				{
					_logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is closed by application. Removing it from pool.");
					_current.Value.Dispose();
					_channels.Remove(_current);
					if (!_channels.Any())
					{
						var newChannelTask = CreateAndWireupAsync();
						newChannelTask.Wait();
						_channels.AddFirst(newChannelTask.Result);
						_current = _channels.First;
						return newChannelTask;
					}
				}
			}

			var openChannel = _channels.FirstOrDefault(c => c.IsOpen);
			if (openChannel != null)
			{
				return Task.FromResult(openChannel);
			}
			var isRecoverable = _channels.Any(c => c is IRecoverable);
			if (!isRecoverable)
			{
				throw new Exception("Unable to retreive channel. All existing channels are closed and none of them are recoverable.");
			}

			var tcs = new TaskCompletionSource<IModel>();
			_requestQueue.Enqueue(tcs);
			return tcs.Task;
		}

		public Task<IModel> CreateChannelAsync(IConnection connection = null)
		{
			return connection != null
				? Task.FromResult(connection.CreateModel())
				: GetConnectionAsync().ContinueWith(tConnection => tConnection.Result.CreateModel());
		}

		private Task<IModel> CreateAndWireupAsync()
		{
			return GetConnectionAsync()
				.ContinueWith(tConnection =>
				{
					var channel = tConnection.Result.CreateModel();
					var recoverable = channel as IRecoverable;
					if (recoverable != null)
					{
						recoverable.Recovery += (sender, args) =>
						{
							if (!_channels.Contains(channel))
							{
								_channels.AddLast(channel);
							}
							while (!_requestQueue.IsEmpty)
							{
								TaskCompletionSource<IModel> requestTcs;
								if (_requestQueue.TryDequeue(out requestTcs))
								{
									requestTcs.TrySetResult(channel);
								}
							}
						};
					}

					return channel;
				});
		}

		private Task<IConnection> GetConnectionAsync()
		{
			if (_connection == null)
			{
				_logger.LogDebug($"Creating a new connection for {_config.Hostnames.Count} hosts.");
				_connection = _connectionFactory.CreateConnection(_config.Hostnames);
			}
			if (_connection.IsOpen)
			{
				_logger.LogDebug("Existing connection is open and will be used.");
				return Task.FromResult(_connection);
			}
			_logger.LogInformation("The existing connection is not open.");

			if (_connection.CloseReason.Initiator == ShutdownInitiator.Application)
			{
				_logger.LogInformation("Connection is closed with Application as initiator. It will not be recovered.");
				_connection.Dispose();
				throw new Exception("Application shutdown is initiated by the Application. A new connection will not be created.");
			}

			var recoverable = _connection as IRecoverable;
			if (recoverable == null)
			{
				_logger.LogInformation("Connection is not recoverable, trying to create a new connection.");
				_connection.Dispose();
				throw new Exception("The non recoverable connection is closed. A channel can not be obtained.");
			}

			_logger.LogDebug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
			var recoverTcs = new TaskCompletionSource<IConnection>();

			EventHandler<EventArgs> completeTask = null;
			completeTask = (sender, args) =>
			{
				_logger.LogDebug("Connection has been recovered!");
				recoverTcs.TrySetResult(recoverable as IConnection);
				recoverable.Recovery -= completeTask;
			};

			recoverable.Recovery += completeTask;
			return recoverTcs.Task;
		}
	}
}
