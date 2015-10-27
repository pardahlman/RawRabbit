using System;
using System.Collections.Concurrent;
using System.Threading;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		IModel GetChannel();
		IModel CreateChannel();
	}

	public class ChannelFactory : IChannelFactory
	{
		private readonly IConnectionBroker _connectionBroker;
		private readonly ConcurrentDictionary<IConnection, ThreadLocal<IModel>> _connectionToChannel;

		public ChannelFactory(IConnectionBroker connectionBroker)
		{
			_connectionBroker = connectionBroker;
			_connectionToChannel = new ConcurrentDictionary<IConnection, ThreadLocal<IModel>>();
		}

		public void Dispose()
		{
			foreach (var connection in _connectionToChannel.Keys)
			{
				connection?.Dispose();
			}
		}

		public void CloseAll()
		{
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
				_connectionToChannel.TryAdd(currentConnection, new ThreadLocal<IModel>(currentConnection.CreateModel));
			}

			var threadChannel = _connectionToChannel[currentConnection];

			if (threadChannel.Value.IsOpen)
			{
				return threadChannel.Value;
			}
			
			threadChannel.Value = _connectionBroker.GetConnection().CreateModel();
			return threadChannel.Value;
		}

		public IModel CreateChannel()
		{
			return _connectionBroker.GetConnection().CreateModel();
		}
	}
}
