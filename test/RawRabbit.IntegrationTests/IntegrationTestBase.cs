using System;
using RabbitMQ.Client;

namespace RawRabbit.IntegrationTests
{
	public class IntegrationTestBase : IDisposable
	{
		protected IModel TestChannel => _testChannel.Value;
		private readonly Lazy<IModel> _testChannel;
		private IConnection _connection;
		
		public IntegrationTestBase()
		{
			_testChannel = new Lazy<IModel>(() =>
			{
				_connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
				return _connection.CreateModel();
			});
		}

		public virtual void Dispose()
		{
			TestChannel?.Dispose();
			_connection?.Dispose();
		}
	}
}
