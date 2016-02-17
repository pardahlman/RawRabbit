using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public class ChannelFactory : IChannelFactory
	{
		private readonly IConnectionFactory _connectionFactory;
		private ThreadLocal<IModel> _threadChannels; 
		private IConnection _connection;
		private readonly ILogger _logger = LogManager.GetLogger<ChannelFactory>();
		private readonly RawRabbitConfiguration _config;
		private readonly ConcurrentDictionary<IModel, DateTime> _accessDictionary;
		private readonly System.Threading.Timer _closeTimer;

		public ChannelFactory(RawRabbitConfiguration config, IConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
			_accessDictionary = new ConcurrentDictionary<IModel, DateTime>();
			_config = config;
			_threadChannels = new ThreadLocal<IModel>(true);

			try
			{
				_logger.LogDebug("Connecting to primary host.");
				_connection = _connectionFactory.CreateConnection(_config.Hostnames);
				_logger.LogInformation("Successfully established connection.");
			}
			catch (BrokerUnreachableException e)
			{
				_logger.LogError("Unable to connect to broker", e);
				throw e.InnerException;
			}
			_closeTimer = new System.Threading.Timer(state =>
			{
				var enumerator = _accessDictionary.GetEnumerator();
				while (enumerator.MoveNext())
				{
					if (DateTime.Now - enumerator.Current.Value > TimeSpan.FromSeconds(10))
					{
						DateTime lastUsed;
						if (_accessDictionary.TryRemove(enumerator.Current.Key, out lastUsed))
						{
							_logger.LogInformation($"Channel {enumerator.Current.Key.ChannelNumber} was last used {lastUsed}. Closing...");
							enumerator.Current.Key.Close();
						}
					}
				}
			}, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
		}

		public void Dispose()
		{
			_connection?.Dispose();
			foreach (var channel in _threadChannels?.Values ?? Enumerable.Empty<IModel>())
			{
				channel?.Dispose();
			}
			_threadChannels?.Dispose();
			_closeTimer?.Dispose();
			_threadChannels = null;
		}

		public IModel GetChannel()
		{
			return GetChannelAsync().Result;
		}

		public Task<IModel> GetChannelAsync()
		{
			if (_threadChannels.Value?.IsOpen ?? false)
			{
				_logger.LogDebug($"Using existing channel with id '{_threadChannels.Value.ChannelNumber}' on thread '{Thread.CurrentThread.ManagedThreadId}'");
				_accessDictionary.AddOrUpdate(_threadChannels.Value, DateTime.Now, (model, time) => DateTime.Now);
				return Task.FromResult(_threadChannels.Value);
			}

			return GetConnectionAsync()
				.ContinueWith(connectionTask => GetOrCreateChannelAsync(connectionTask.Result))
				.Unwrap()
				.ContinueWith(tChannel =>
				{
					_accessDictionary.AddOrUpdate(tChannel.Result, DateTime.Now, (model, time) => DateTime.Now);
					return tChannel.Result;
				});
		}

		private Task<IModel> GetOrCreateChannelAsync(IConnection connection)
		{
			if (!connection?.IsOpen ?? true)
			{
				_logger.LogInformation("Connection is not open or defined. Waiting for a open connection.");
				return GetConnectionAsync()
						.ContinueWith(c => GetOrCreateChannelAsync(c.Result))
						.Unwrap();
			}
			if (_threadChannels.Value == null)
			{
				_logger.LogInformation($"Creating a new channel for thread with id '{Thread.CurrentThread.ManagedThreadId}'");
				_threadChannels.Value = connection.CreateModel();
				if (_config.AutoCloseConnection && !connection.AutoClose)
				{
					_logger.LogInformation($"Setting AutoClose to true for current connection");
					connection.AutoClose = _config.AutoCloseConnection;
				}
				return Task.FromResult(_threadChannels.Value);
			}
			if (_threadChannels.Value.IsOpen)
			{
				_logger.LogDebug($"Using open channel with id {_threadChannels.Value.ChannelNumber}");
				return Task.FromResult(_threadChannels.Value);
			}

			_logger.LogInformation($"Channel {_threadChannels.Value.ChannelNumber} is closed.");
			var recoveryChannel = _threadChannels.Value as IRecoverable;
			if (recoveryChannel == null)
			{
				_logger.LogInformation("Channel is not recoverable. Opening a new channel.");
				_threadChannels.Value.Dispose();
				_threadChannels.Value = connection.CreateModel();
				return Task.FromResult(_threadChannels.Value);
			}

			_logger.LogDebug("Channel is recoverable. Waiting for 'Recovery' event to be triggered.");
			var recoverTcs = new TaskCompletionSource<IModel>();
			recoveryChannel.Recovery += (sender, args) =>
			{
				recoverTcs.SetResult(recoveryChannel as IModel);
			};
			return recoverTcs.Task;
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
			var recoverable = _connection as IRecoverable;
			if (recoverable == null)
			{
				_logger.LogInformation("Connection is not recoverable, trying to create a new connection.");
				_connection.Dispose();
				try
				{
					_connection = _connectionFactory.CreateConnection(_config.Hostnames);
					return Task.FromResult(_connection);
				}
				catch (BrokerUnreachableException)
				{
					_logger.LogInformation("None of the hosts are reachable. Waiting five seconds and try again.");
					return Task
						.Delay(TimeSpan.FromSeconds(5))
						.ContinueWith(t => _connectionFactory.CreateConnection(_config.Hostnames));
				}
			}

			_logger.LogDebug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
			var recoverTcs = new TaskCompletionSource<IConnection>();
			recoverable.Recovery += (sender, args) =>
			{
				_logger.LogDebug("Connection has been recovered!");
				recoverTcs.TrySetResult(recoverable as IConnection);
			};
			return recoverTcs.Task;
		}

		public IModel CreateChannel(IConnection connection = null)
		{
			return CreateChannelAsync(connection).Result;
		}

		public Task<IModel> CreateChannelAsync(IConnection connection = null)
		{
			var connectionTask = connection != null
				? Task.FromResult(connection)
				: GetConnectionAsync();
			return connectionTask.ContinueWith(c => c.Result.CreateModel());
		}
	}
}
