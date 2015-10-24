using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConfigChannelFactory : IChannelFactory
	{
		private readonly Func<IConnection> _connectionFn;
		private IConnection _connection;
		private readonly ThreadLocal<IModel> _threadChannal;

		public ConfigChannelFactory(IConnectionFactory connectionFactory)
		{
			_connectionFn = () => CreateConnection(connectionFactory);
			_connection = _connectionFn();
			_threadChannal = new ThreadLocal<IModel>(_connection.CreateModel);
			_connection.ConnectionShutdown += (sender, args) =>
			{
				_connection = _connectionFn();
			};
		}

		public void Dispose()
		{
			foreach (var channel in _threadChannal.Values)
			{
				if (channel?.IsOpen ?? false)
				{
					channel.Close();
				}
				if (_connection?.IsOpen ?? false)
				{
					_connection.Close();
				}
			}
		}

		public IModel GetChannel()
		{
			if (_threadChannal.Value.IsOpen)
			{
				return _threadChannal.Value;
			}
			if (!_connection.IsOpen)
			{
				_connection = _connectionFn();
			}
			_threadChannal.Value = _connection.CreateModel();
			return _threadChannal.Value;
		}

		private static IConnection CreateConnection(IConnectionFactory factory)
		{
			try
			{
				return factory.CreateConnection();
			}
			catch (BrokerUnreachableException e)
			{
				if (e.InnerException is AuthenticationFailureException)
				{
					throw e.InnerException;
				}
				throw;
			}
		}
	}
}
