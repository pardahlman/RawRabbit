using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
    public class ChannelFactory : IChannelFactory
    {
        private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _requestQueue;
        private readonly ILogger _logger = LogManager.GetLogger<ChannelFactory>();
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
                _logger.LogError("Unable to connect to broker", e);
                throw e.InnerException;
            }
        }

        protected virtual void SetupConnectionRecovery(IConnection connection = null)
        {
            connection = connection ?? _connection;
            var recoverable = connection as IRecoverable;
            if (recoverable == null)
            {
                _logger.LogInformation("Connection is not Recoverable. Failed connection will cause unhandled exception to be thrown.");
                return;
            }
            _logger.LogDebug("Setting up Connection Recovery");
            recoverable.Recovery += (sender, args) =>
            {
                _logger.LogInformation($"Connection has been recovered. Starting channel processing.");
                EnsureRequestsAreHandled();
            };
        }

        internal virtual void Initialize()
        {
            _logger.LogDebug($"Initiating {_channelConfig.InitialChannelCount} channels.");
            for (var i = 0; i < _channelConfig.InitialChannelCount; i++)
            {
                if (i > _channelConfig.MaxChannelCount)
                {
                    _logger.LogDebug($"Trying to create channel number {i}, but max allowed channels are {_channelConfig.MaxChannelCount}");
                    continue;
                }
                CreateAndWireupAsync().Wait();
            }
            _current = _channels.First;

            if (_channelConfig.EnableScaleDown || _channelConfig.EnableScaleUp)
            {
                _logger.LogInformation($"Scaling is enabled with interval set to {_channelConfig.ScaleInterval}.");
                _scaleTimer = new Timer(state =>
                {
                    AdjustChannelCount(_channels.Count, _requestQueue.Count);
                }, null, _channelConfig.ScaleInterval, _channelConfig.ScaleInterval);
            }
            else
            {
                _logger.LogInformation("Channel scaling is disabled.");
            }
        }

        internal virtual void AdjustChannelCount(int channelCount, int requestCount)
        {
            if (channelCount == 0)
            {
                _logger.LogWarning("Channel count is 0. Skipping channel scaling.");
                return;
            }

            var workPerChannel = requestCount / channelCount;
            var canCreateChannel = channelCount < _channelConfig.MaxChannelCount;
            var canCloseChannel = channelCount > 1;
            _logger.LogDebug($"Begining channel scaling.\n  Channel count: {channelCount}\n  Work per channel: {workPerChannel}");

            if (_channelConfig.EnableScaleUp && canCreateChannel && workPerChannel > _channelConfig.WorkThreshold)
            {
                CreateAndWireupAsync();
                return;
            }
            if (_channelConfig.EnableScaleDown && canCloseChannel && requestCount == 0)
            {
                var toClose = _channels.Last.Value;
                _logger.LogInformation($"Channel '{toClose.ChannelNumber}' will be closed in {_channelConfig.GracefulCloseInterval}.");
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

        public IModel CreateChannel(IConnection connection = null)
        {
            return CreateChannelAsync(connection).Result;
        }

        public Task<IModel> GetChannelAsync()
        {
            var tcs = new TaskCompletionSource<IModel>();
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
                _logger.LogInformation("Currently no available channels.");
                return;
            }
            lock (_processLock)
            {
                if (_processingRequests)
                {
                    return;
                }
                _processingRequests = true;
                _logger.LogDebug("Begining to process 'GetChannel' requests.");
            }

            TaskCompletionSource<IModel> channelTcs;
            while (_requestQueue.TryDequeue(out channelTcs))
            {
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

                    _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is closed. Removing it from pool.");
                    _channels.Remove(_current);

                    if (_current.Value.CloseReason.Initiator == ShutdownInitiator.Application)
                    {
                        _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is closed by application. Disposing channel.");
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
                    _logger.LogInformation($"Using channel '{openChannel.ChannelNumber}', which is open.");
                    channelTcs.TrySetResult(openChannel);
                    continue;
                }
                var isRecoverable = _channels.Any(c => c is IRecoverable);
                if (!isRecoverable)
                {
                    _processingRequests = false;
                    throw new ChannelAvailabilityException("Unable to retreive channel. All existing channels are closed and none of them are recoverable.");
                }

                _logger.LogInformation("Unable to find an open channel. Requeue TaskCompletionSource for future process and abort execution.");
                _requestQueue.Enqueue(channelTcs);
                _processingRequests = false;
                return;
            }
            _processingRequests = false;
            _logger.LogDebug("'GetChannel' has been processed.");
        }

        public Task<IModel> CreateChannelAsync(IConnection connection = null)
        {
            return connection != null
                ? Task.FromResult(connection.CreateModel())
                : GetConnectionAsync().ContinueWith(tConnection => tConnection.Result.CreateModel());
        }

        internal virtual Task<IModel> CreateAndWireupAsync()
        {
            return GetConnectionAsync()
                .ContinueWith(tConnection =>
                {
                    var channel = tConnection.Result.CreateModel();
                    _logger.LogInformation($"Channel '{channel.ChannelNumber}' has been created.");
                    var recoverable = channel as IRecoverable;
                    if (recoverable != null)
                    {
                        recoverable.Recovery += (sender, args) =>
                        {
                            if (!_channels.Contains(channel))
                            {
                                _logger.LogInformation($"Channel '{_current.Value.ChannelNumber}' is recovered. Adding it to pool.");
                                _channels.AddLast(channel);
                            }
                        };
                    }
                    _channels.AddLast(new LinkedListNode<IModel>(channel));
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
