using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public class ChannelFactory : IChannelFactory
	{
		private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _requestQueue;
		private readonly ILog _logger = LogProvider.For<ChannelFactory>();
		internal readonly ChannelFactoryConfiguration _channelConfig;
		private readonly IConnectionFactory _connectionFactory;
		private readonly RawRabbitConfiguration _config;
		internal readonly LinkedList<IModel> _channels;
		private LinkedListNode<IModel> _current;
		private readonly object _channelLock = new object();
		private readonly object _processLock = new object();
		private Timer _scaleTimer;
		private IConnection _connection;
		private bool _processingRequests;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ChannelFactoryConfiguration channelConfig)
		{
			_connectionFactory = connectionFactory;
			_config = config;
			_channelConfig = channelConfig;
			_requestQueue = new ConcurrentQueue<TaskCompletionSource<IModel>>();
			_channels = new LinkedList<IModel>();

			ConnectToBroker();
			Initialize();
		}

		protected virtual void ConnectToBroker()
		{
			try
			{
				_connection = _connectionFactory.CreateConnection(_config.Hostnames);
				SetupConnectionRecovery(_connection);
			}
			catch (BrokerUnreachableException e)
			{
				_logger.Info("Unable to connect to broker", e);
				throw e.InnerException;
			}
		}

		protected virtual void SetupConnectionRecovery(IConnection connection = null)
		{
			connection = connection ?? _connection;
			var recoverable = connection as IRecoverable;
			if (recoverable == null)
			{
				_logger.Info("Connection is not Recoverable. Failed connection will cause unhandled exception to be thrown.");
				return;
			}
			_logger.Debug("Setting up Connection Recovery");
			recoverable.Recovery += (sender, args) =>
			{
				_logger.Info($"Connection has been recovered. Starting channel processing.");
				EnsureRequestsAreHandled();
			};
		}

		internal virtual void Initialize()
		{
			_logger.Debug($"Initiating {_channelConfig.InitialChannelCount} channels.");
			for (var i = 0; i < _channelConfig.InitialChannelCount; i++)
			{
				if (i > _channelConfig.MaxChannelCount)
				{
					_logger.Debug("Trying to create channel number {channelIndex}, but max allowed channels are {maxChannelCount}", i, _channelConfig.MaxChannelCount);
					continue;
				}
				CreateAndWireupAsync().Wait();
			}
			_current = _channels.First;

			if (_channelConfig.EnableScaleDown || _channelConfig.EnableScaleUp)
			{
				_logger.Info("Scaling is enabled with interval set to {channelScaleInterval}.", _channelConfig.ScaleInterval);
				_scaleTimer = new Timer(state =>
				{
					AdjustChannelCount(_channels.Count, _requestQueue.Count);
				}, null, _channelConfig.ScaleInterval, _channelConfig.ScaleInterval);
			}
			else
			{
				_logger.Info("Channel scaling is disabled.");
			}
		}

		internal virtual void AdjustChannelCount(int channelCount, int requestCount)
		{
			if (channelCount == 0)
			{
				_logger.Warn("Channel count is 0. Skipping channel scaling.");
				return;
			}

			var workPerChannel = requestCount / channelCount;
			var canCreateChannel = channelCount < _channelConfig.MaxChannelCount;
			var canCloseChannel = channelCount > 1;
			_logger.Debug("Begining channel scaling.\n  Channel count: {channelCount}\n  Work per channel: {workPerChannel}", channelCount, workPerChannel);

			if (_channelConfig.EnableScaleUp && canCreateChannel && workPerChannel > _channelConfig.WorkThreshold)
			{
				CreateAndWireupAsync();
				return;
			}
			if (_channelConfig.EnableScaleDown && canCloseChannel && requestCount == 0)
			{
				var toClose = _channels.Last.Value;
				_logger.Info("Channel '{channelNumber}' will be closed in {gracefulCloseInterval}.", toClose.ChannelNumber, _channelConfig.GracefulCloseInterval);
				_channels.Remove(toClose);

				Timer graceful = null;
				graceful = new Timer(state =>
				{
					graceful?.Dispose();
					toClose.Dispose();
				}, null, _channelConfig.GracefulCloseInterval, new TimeSpan(-1));
			}
		}

		public void Dispose()
		{
			foreach (var channel in _channels)
			{
				channel?.Dispose();
			}
			_connection?.Dispose();
			_scaleTimer?.Dispose();
		}

		public IModel GetChannel()
		{
			return GetChannelAsync().Result;
		}

		public Task<IModel> GetChannelAsync(CancellationToken token = default(CancellationToken))
		{
			var tcs = new TaskCompletionSource<IModel>();
			token.Register(() => tcs.TrySetCanceled());
			_requestQueue.Enqueue(tcs);
			if (_connection.IsOpen)
			{
				EnsureRequestsAreHandled();
			}
			else
			{
				var recoverable = _connection as IRecoverable;
				if (recoverable == null)
				{
					throw new ChannelAvailabilityException("Unable to retrieve chanel. Connection to broker is closed and not recoverable.");
				}
			}
			return tcs.Task;
		}

		private void EnsureRequestsAreHandled()
		{
			if (_processingRequests)
			{
				return;
			}
			if (!_channels.Any() && _channelConfig.InitialChannelCount > 0)
			{
				_logger.Info("Currently no available channels.");
				return;
			}
			lock (_processLock)
			{
				if (_processingRequests)
				{
					return;
				}
				_processingRequests = true;
				_logger.Debug("Begining to process 'GetChannel' requests.");
			}

			TaskCompletionSource<IModel> channelTcs;
			while (_requestQueue.TryDequeue(out channelTcs))
			{
				if (channelTcs.Task.IsCanceled)
				{
					continue;
				}
				lock (_channelLock)
				{
					if (_current == null && _channelConfig.InitialChannelCount == 0)
					{
						CreateAndWireupAsync().Wait();
						_current = _channels.First;
					}
					_current = _current.Next ?? _channels.First;

					if (_current.Value.IsOpen)
					{
						channelTcs.TrySetResult(_current.Value);
						continue;
					}

					_logger.Info("Channel '{channelNumber}' is closed. Removing it from pool.", _current.Value.ChannelNumber);
					_channels.Remove(_current);

					if (_current.Value.CloseReason.Initiator == ShutdownInitiator.Application)
					{
						_logger.Info("Channel '{channelNumber}' is closed by application. Disposing channel.", _current.Value.ChannelNumber);
						_current.Value.Dispose();
						if (!_channels.Any())
						{
							var newChannelTask = CreateAndWireupAsync();
							newChannelTask.Wait();
							_current = _channels.Last;
							channelTcs.TrySetResult(_current.Value);
							continue;
						}
					}
				}

				var openChannel = _channels.FirstOrDefault(c => c.IsOpen);
				if (openChannel != null)
				{
					_logger.Info("Using channel '{channelNumber}', which is open.", openChannel.ChannelNumber);
					channelTcs.TrySetResult(openChannel);
					continue;
				}
				var isRecoverable = _channels.Any(c => c is IRecoverable);
				if (!isRecoverable)
				{
					_processingRequests = false;
					throw new ChannelAvailabilityException("Unable to retreive channel. All existing channels are closed and none of them are recoverable.");
				}

				_logger.Info("Unable to find an open channel. Requeue TaskCompletionSource for future process and abort execution.");
				_requestQueue.Enqueue(channelTcs);
				_processingRequests = false;
				return;
			}
			_processingRequests = false;
			_logger.Debug("'GetChannel' has been processed.");
		}

		public async Task<IModel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			token.ThrowIfCancellationRequested();
			var connection = await GetConnectionAsync(token);
			return connection.CreateModel();
		}

		internal virtual async Task<IModel> CreateAndWireupAsync(CancellationToken token = default(CancellationToken))
		{
			var connection = await GetConnectionAsync(token);
			var channel = connection.CreateModel();
			if (_config.AutoCloseConnection && !connection.AutoClose)
			{
				connection.AutoClose = true;
			}
			_logger.Info("Channel '{channelNumber}' has been created.", channel.ChannelNumber);
			var recoverable = channel as IRecoverable;
			if (recoverable != null)
			{
				recoverable.Recovery += (sender, args) =>
				{
					if (!_channels.Contains(channel))
					{
						_logger.Info("Channel '{channelNumber}' is recovered. Adding it to pool.", _current.Value.ChannelNumber);
						_channels.AddLast(channel);
					}
				};
			}
			_channels.AddLast(new LinkedListNode<IModel>(channel));
			return channel;
		}

		private Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			token.ThrowIfCancellationRequested();
			if (_connection == null)
			{
				_logger.Debug("Creating a new connection for {hostNameCount} hosts.", _config.Hostnames.Count);
				_connection = _connectionFactory.CreateConnection(_config.Hostnames);
			}
			if (_connection.IsOpen)
			{
				_logger.Debug("Existing connection is open and will be used.");
				return Task.FromResult(_connection);
			}
			_logger.Info("The existing connection is not open.");

			if (_connection.CloseReason.Initiator == ShutdownInitiator.Application)
			{
				_logger.Info("Connection is closed with Application as initiator. It will not be recovered.");
				_connection.Dispose();
				throw new Exception("Application shutdown is initiated by the Application. A new connection will not be created.");
			}

			var recoverable = _connection as IRecoverable;
			if (recoverable == null)
			{
				_logger.Info("Connection is not recoverable, trying to create a new connection.");
				_connection.Dispose();
				throw new Exception("The non recoverable connection is closed. A channel can not be obtained.");
			}

			_logger.Debug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
			var recoverTcs = new TaskCompletionSource<IConnection>();

			EventHandler<EventArgs> completeTask = null;
			completeTask = (sender, args) =>
			{
				_logger.Debug("Connection has been recovered!");
				recoverTcs.TrySetResult(recoverable as IConnection);
				recoverable.Recovery -= completeTask;
			};

			recoverable.Recovery += completeTask;
			return recoverTcs.Task;
		}
	}

	public class ChannelFactoryConfiguration
	{
		public bool EnableScaleUp { get; set; }
		public bool EnableScaleDown { get; set; }
		public TimeSpan ScaleInterval { get; set; }
		public TimeSpan GracefulCloseInterval { get; set; }
		public int MaxChannelCount { get; set; }
		public int InitialChannelCount { get; set; }
		public int WorkThreshold { get; set; }

		public static ChannelFactoryConfiguration Default => new ChannelFactoryConfiguration
		{
			InitialChannelCount = 0,
			MaxChannelCount = 1,
			GracefulCloseInterval = TimeSpan.FromMinutes(30),
			WorkThreshold = 20000,
			ScaleInterval = TimeSpan.FromSeconds(10),
			EnableScaleUp = false,
			EnableScaleDown = false
		};
	}
}
