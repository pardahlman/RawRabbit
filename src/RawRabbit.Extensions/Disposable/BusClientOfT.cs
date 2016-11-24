using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Legacy.Publish;
using RawRabbit.Configuration.Legacy.Request;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Legacy.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Extensions.Disposable
{
	public interface ILegacyBusClient<out TMessageContext> : RawRabbit.ILegacyBusClient<TMessageContext>, IDisposable where TMessageContext : IMessageContext
	{ }

	public class LegacyBusClient<TMessageContext> : ILegacyBusClient<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly Client.ILegacyBusClient<TMessageContext> _client;

		public LegacyBusClient(Client.ILegacyBusClient<TMessageContext> client)
		{
			_client = client;
		}

		public void Dispose()
		{
			var shutDownTask = ShutdownAsync(TimeSpan.Zero);
			shutDownTask.GetAwaiter().GetResult();
		}

		#region Pass-through
		public Task ShutdownAsync(TimeSpan? graceful = null)
		{
			return _client.ShutdownAsync(graceful);
		}

		public ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return _client.SubscribeAsync(subscribeMethod, configuration);
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			return _client.PublishAsync(message, globalMessageId, configuration);
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			return _client.RespondAsync(onMessage, configuration);
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(),
			Action<IRequestConfigurationBuilder> configuration = null)
		{
			return _client.RequestAsync<TRequest, TResponse>(message, globalMessageId, configuration);
		}
		#endregion
	}
}
