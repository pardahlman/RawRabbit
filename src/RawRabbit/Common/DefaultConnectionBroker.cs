using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public class DefaultConnectionBroker : IConnectionBroker
	{
		private readonly TimeSpan _retryInterval;
		private readonly List<IConnectionFactory> _factories;
		private readonly ConcurrentDictionary<IConnectionFactory, IConnection> _factoryToConnection;
		private const int PrimaryIndex = 0;
		private int _currentIndex = PrimaryIndex;
		private readonly ILogger _logger = LogManager.GetLogger<DefaultConnectionBroker>();

		public DefaultConnectionBroker(IEnumerable<IConnectionFactory> connectionFactories, TimeSpan retryInterval)
		{
			_retryInterval = retryInterval;
			_factories = connectionFactories.Where(f => f != null).ToList();
			_logger.LogInformation($"Preparing connection broker for {_factories.Count} potential brokers.");

			if (!_factories.Any())
			{
				throw new ArgumentException($"{nameof(connectionFactories)} must contain at leaste one broker configuration.");
			}

			_factoryToConnection = new ConcurrentDictionary<IConnectionFactory, IConnection>();

			IConnection primary;
			try
			{
				_logger.LogDebug("Connecting to primary host.");
				primary = _factories[0].CreateConnection();
				SetupLoggin(primary);
				_logger.LogInformation($"Successfully established connection to {primary.Endpoint.HostName}.");
			}
			catch (BrokerUnreachableException e)
			{
				_logger.LogError("Unable to connect to broker", e);
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
				_logger.LogInformation("Unable to establish connection to primary broker. Attempting to connect to secondaries.");
				if (TryGetConnection(secondaryIndex, out connection))
				{
					_currentIndex = secondaryIndex;

					Timer retryPrimaryTimer = null;
					retryPrimaryTimer = new Timer(state =>
					{
						_logger.LogDebug("Retry interval elapsed. Will retry with primary host for next connection.");
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
			_logger.LogDebug($"Retrieving or creating connection from factory with index '{factoryIndex}'.");
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
					var newConnection = factory.CreateConnection();
					SetupLoggin(newConnection);
					_factoryToConnection.TryAdd(factory, newConnection);
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
				_logger.LogWarning(
					$"The previously used connection to {existingConnection.Endpoint.HostName} has beeen closed. Attempting to reconnect");
				try
				{
					var newConnection = factory.CreateConnection();
					SetupLoggin(newConnection);
					_factoryToConnection.TryAdd(factory, newConnection);
				}
				catch (BrokerUnreachableException e)
				{
					connection = null;
					return false;
				}
			}
			else
			{
				_logger.LogInformation($"Using existing connection to '{existingConnection.Endpoint.HostName}'.");
			}

			connection = _factoryToConnection[factory];
			_logger.LogDebug($"Using connection to {connection.Endpoint.HostName}.");
			return connection.IsOpen;
		}


		private void SetupLoggin(IConnection primary)
		{
			primary.ConnectionShutdown +=
				(sender, args) =>
					_logger.LogInformation(
						$"Connection to host '{primary.Endpoint.HostName}' was shot down.\n  Cause: {args.Cause}\n  Reason: {args.ReplyText}\n  Initiator: {args.Initiator}"
					);
			primary.ConnectionBlocked +=
					(sender, args) => _logger.LogInformation($"The connection is block. The reason is '{args.Reason}'"
				);
			primary.CallbackException +=
					(sender, args) => _logger.LogInformation($"The callback threw an exception, '{args.Detail}'. \n{args.Exception}"
				);
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
