using System;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		IModel GetChannel();
	}

	public class ChannelFactory : IChannelFactory
	{
		private readonly Func<IConnection> _connectionFn;
		private IConnection _connection;
		private readonly ThreadLocal<IModel> _threadChannal;

		public ChannelFactory(IConnectionFactory connectionFactory)
		{
			_connectionFn = () => CreateConnection(connectionFactory);
			_connection = _connectionFn();
			_threadChannal = new ThreadLocal<IModel>(_connection.CreateModel, true);
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

		public void CloseAll()
		{
			foreach (var channel in _threadChannal.Values)
			{
				channel?.Close();
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
