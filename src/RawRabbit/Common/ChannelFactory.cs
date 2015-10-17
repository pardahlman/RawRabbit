using System;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		IModel GetChannel();
	}

	public class ChannelFactory : IChannelFactory
	{
		private readonly IConnection _connection;
		private IModel _channel;

		public ChannelFactory(IConnection connection)
		{
			_connection = connection;
		}

		public IModel GetChannel()
		{
			return _channel ?? (_channel = _connection.CreateModel());
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}
	}
}
