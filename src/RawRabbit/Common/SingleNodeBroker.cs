using System;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public interface IConnectionBroker: IDisposable
	{
		IConnection GetConnection();
	}

	public class SingleNodeBroker : IConnectionBroker
	{
		private readonly IConnectionFactory _factory;
		private IConnection _connection;

		public SingleNodeBroker(BrokerConfiguration config = null)
		{
			config = config ?? BrokerConfiguration.Local;
			_factory = new ConnectionFactory
			{
				HostName = config.Hostname,
				VirtualHost = config.VirtualHost,
				Password = config.Password,
				UserName = config.Username
			};
			_connection = _factory.CreateConnection();
		}

		public IConnection GetConnection()
		{
			if (_connection.IsOpen)
			{
				return _connection;
			}
			_connection.Dispose();

			//TODO: handle connection exception, reconnect, ...
			_connection = _factory.CreateConnection();
			return _connection;
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}
	}
}
