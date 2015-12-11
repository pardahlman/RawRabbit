using System;
using System.Collections.Concurrent;
using System.Threading;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		/// <summary>
		/// Retrieves a channel that is disposed by the channel factory
		/// </summary>
		/// <returns>A new or existing instance of an IModel</returns>
		IModel GetChannel();
		/// <summary>
		/// Creates a new istance of a channal that the caller is responsible
		/// in closing and disposing.
		/// </summary>
		/// <returns>A new instance of an IModel</returns>
		IModel CreateChannel(IConnection connection = null);
	}

	public class ChannelFactory : IChannelFactory
	{
		private readonly IConnectionBroker _connectionBroker;
		private readonly ConcurrentDictionary<IConnection, ThreadLocal<IModel>> _connectionToChannel;
		private readonly bool _autoClose;
		private readonly ILogger _logger = LogManager.GetLogger<ChannelFactory>();

		public ChannelFactory(IConnectionBroker connectionBroker, RawRabbitConfiguration config)
		{
			_connectionBroker = connectionBroker;
			_connectionToChannel = new ConcurrentDictionary<IConnection, ThreadLocal<IModel>>();
			_autoClose = config.AutoCloseConnection;
		}

		public void Dispose()
		{
			_connectionBroker?.Dispose();
		}

		public void CloseAll()
		{
			_logger.LogDebug("Trying to close all connections.");
			foreach (var connection in _connectionToChannel.Keys)
			{
				connection?.Close();
			}
		}

		public IModel GetChannel()
		{
			var currentConnection = _connectionBroker.GetConnection();

			if (!_connectionToChannel.ContainsKey(currentConnection))
			{
				_connectionToChannel.TryAdd(currentConnection, new ThreadLocal<IModel>());
				var newChannel = CreateChannel(currentConnection);
				_connectionToChannel[currentConnection].Value = newChannel;
			}

			var threadChannel = _connectionToChannel[currentConnection];
			if (threadChannel.Value.IsOpen)
			{
				return threadChannel.Value;
			}

			var channel = CreateChannel(currentConnection);

			threadChannel.Value?.Dispose();
			threadChannel.Value = channel;

			return threadChannel.Value;
		}

		public IModel CreateChannel(IConnection connection = null)
		{
			connection = connection ?? _connectionBroker.GetConnection();
			var channel = connection.CreateModel();
			if (_autoClose && !connection.AutoClose)
			{
				_logger.LogInformation("Setting AutoClose to true for connection while calling 'CreateChannel'.");
				connection.AutoClose = true;
			}
			else
			{
				_logger.LogDebug($"AutoClose in settings object is set to: '{_autoClose}' and on connection '{connection.AutoClose}'");
			}
			return channel;
		}
	}
}
