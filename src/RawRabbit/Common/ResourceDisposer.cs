using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Subscription;

namespace RawRabbit.Common
{
	public interface IResourceDisposer :IDisposable
	{
		Task ShutdownAsync(TimeSpan? graceful = null);
	}

	public class ResourceDisposer : IResourceDisposer
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConnectionFactory _connectionFactory;
		private readonly ISubscriptionRepository _subscriptionRepo;
		private readonly RawRabbitConfiguration _config;

		public ResourceDisposer(
			IChannelFactory channelFactory,
			IConnectionFactory connectionFactory,
			ISubscriptionRepository subscriptionRepo,
			RawRabbitConfiguration config)
		{
			_channelFactory = channelFactory;
			_connectionFactory = connectionFactory;
			_subscriptionRepo = subscriptionRepo;
			_config = config;
		}

		public void Dispose()
		{
			_channelFactory.Dispose();
			(_connectionFactory as IDisposable)?.Dispose();
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			var subscriptions = _subscriptionRepo.GetAll();
			foreach (var subscription in subscriptions)
			{
				subscription?.Dispose();
			}
			graceful = graceful ?? _config.GracefulShutdown;
			await Task.Delay(graceful.Value);
			Dispose();
		}
	}
}
