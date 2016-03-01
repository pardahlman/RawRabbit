using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class RoundRobinChannelFactory : IChannelFactory
	{
		private const int MaxChannels = 10;
		private int _currentIndex;
		private readonly object _channelLock = new object();
		private readonly List<IModel> _channels;
		private readonly IConnection _connection;

		public RoundRobinChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config)
		{
			_channels = new List<IModel>();
			_connection = connectionFactory.CreateConnection(config.Hostnames);
			for (var i = 0; i < MaxChannels; i++)
			{
				_channels.Add(_connection.CreateModel());
			}
			_connection.AutoClose = config.AutoCloseConnection;
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}

		public IModel GetChannel()
		{
			return GetChannelAsync().Result;
		}

		public IModel CreateChannel(IConnection connection = null)
		{
			return (connection ?? _connection).CreateModel();
		}

		public Task<IModel> GetChannelAsync()
		{
			lock (_channelLock)
			{
				_currentIndex = (_currentIndex + 1) % MaxChannels;
				var channel = _channels[_currentIndex];
				if (channel.IsOpen)
				{
					return Task.FromResult(channel);
				}
				if (channel.CloseReason.Initiator == ShutdownInitiator.Application)
				{
					channel.Dispose();
					channel = _connection.CreateModel();
					_channels[_currentIndex] = channel;
					return Task.FromResult(channel);
				}
				var recoverable = channel as IRecoverable;
				if (recoverable == null)
				{
					channel.Dispose();
					channel = _connection.CreateModel();
					_channels[_currentIndex] = channel;
					return Task.FromResult(channel);
				}
				var channelTcs = new TaskCompletionSource<IModel>();
				EventHandler<EventArgs> completeTask = null;
				completeTask = (sender, args) =>
				{
					channelTcs.TrySetResult(sender as IModel);
					recoverable.Recovery -= completeTask;
				};
				recoverable.Recovery += completeTask;
				return channelTcs.Task;
			}
		}

		public Task<IModel> CreateChannelAsync(IConnection connection = null)
		{
			return Task.FromResult(CreateChannel(connection));
		}
	}
}
