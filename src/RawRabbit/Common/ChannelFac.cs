using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ChannelFac : IChannelFactory
	{
		private readonly List<ConnectionFactory> _connectionFactories;
		private IDictionary<ConnectionFactory, IConnection> _factoryToConnection;

		public ChannelFac(IEnumerable<ConnectionConfiguration> configs)
		{
			_factoryToConnection = new Dictionary<ConnectionFactory, IConnection>();
			_connectionFactories = new List<ConnectionFactory>();
			foreach (var config in configs)
			{
				_connectionFactories.Add(new ConnectionFactory
				{
					HostName = config.Hostname,
					VirtualHost = config.VirtualHost,
					AutomaticRecoveryEnabled = true,
				});
			}
		}
		

		public void Dispose()
		{
			foreach (var factory in _connectionFactories)
			{
				
			}
		}

		public IModel GetChannel()
		{
			throw new System.NotImplementedException();
		}
	}
}
