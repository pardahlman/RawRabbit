using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RawRabbit.Common
{
	public class DefaultConnectionBroker : IConnectionBroker
	{
		private readonly TimeSpan _retryInterval;
		private readonly List<IConnectionFactory> _factories;
		private readonly ConcurrentDictionary<IConnectionFactory, IConnection> _factoryToConnection;
		private const int PrimaryIndex = 0;
		private int _currentIndex = PrimaryIndex;

		public DefaultConnectionBroker(IEnumerable<IConnectionFactory> connectionFactories, TimeSpan retryInterval)
		{
			_retryInterval = retryInterval;
			_factories = connectionFactories.Where(f => f != null).ToList();

			if (!_factories.Any())
			{
				throw new ArgumentException($"{nameof(connectionFactories)} must contain at leaste one broker configuration.");
			}

			_factoryToConnection = new ConcurrentDictionary<IConnectionFactory, IConnection>();
			
			IConnection primary;
			try
			{
				primary = _factories[0].CreateConnection();
			}
			catch (BrokerUnreachableException e)
			{
				throw e.InnerException;
			}
			_factoryToConnection.TryAdd(_factories[0], primary);
		}

		public IConnection GetConnection()
		{
			IConnection connection;
			if (TryGetConnection(_currentIndex, out connection))
			{
				return connection;
			}
			for (var secondaryIndex = 1; secondaryIndex < _factories.Count; secondaryIndex++)
			{
				if (TryGetConnection(secondaryIndex, out connection))
				{
					_currentIndex = secondaryIndex;

					Timer retryPrimaryTimer = null;
					retryPrimaryTimer = new Timer(state =>
					{
						retryPrimaryTimer?.Dispose();
						_currentIndex = PrimaryIndex;
					}, null, _retryInterval, TimeSpan.FromMilliseconds(-1));

					return connection;
				}
			}
			throw new Exception("Could not connect to any of the brokers.");
		}

		private bool TryGetConnection(int factoryIndex, out IConnection connection)
		{
			if (factoryIndex > _factories.Count - 1)
			{
				connection = null;
				return false;
			}

			var factory = _factories[factoryIndex];
			if (!_factoryToConnection.ContainsKey(factory))
			{
				try
				{
					_factoryToConnection.TryAdd(factory, factory.CreateConnection());
				}
				catch (BrokerUnreachableException)
				{
					connection = null;
					return false;
				}
			}

			var existingConnection = _factoryToConnection[factory];
			if (!existingConnection.IsOpen)
			{
				try
				{
					_factoryToConnection.TryAdd(factory, factory.CreateConnection());
				}
				catch (BrokerUnreachableException e)
				{
					connection = null;
					return false;
				}
			}

			connection = _factoryToConnection[factory];
			return connection.IsOpen;
		}

		public void Dispose()
		{
			foreach (var connection in _factoryToConnection.Values)
			{
				connection?.Dispose();
			}
		}
	}
}
