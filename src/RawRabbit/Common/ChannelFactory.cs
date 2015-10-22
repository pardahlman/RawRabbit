using System;
using System.Threading;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		IModel GetChannel();
	}

	public class ChannelFactory : IChannelFactory
	{
		private readonly ThreadLocal<IModel> _threadChannal; 
		private readonly IConnection _connection;
	
		public ChannelFactory(IConnection connection)
		{
			_connection = connection;
			_threadChannal = new ThreadLocal<IModel>(connection.CreateModel);
		}

		public IModel GetChannel()
		{
			if (_threadChannal.Value.IsOpen)
			{
				return _threadChannal.Value;
			}
			_threadChannal.Value = _connection.CreateModel();
			return _threadChannal.Value;
		}

		public void Dispose()
		{
			_connection?.Dispose();
			_threadChannal?.Dispose();
		}
	}
}
