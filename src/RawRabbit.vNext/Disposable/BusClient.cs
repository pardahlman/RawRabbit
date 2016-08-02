using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.vNext.Disposable
{
	public interface IBusClient : RawRabbit.IBusClient, IDisposable { }

	public class BusClient : IBusClient
	{
		private readonly RawRabbit.IBusClient _client;

		public BusClient(RawRabbit.IBusClient client)
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

		public ISubscription SubscribeAsync<T>(Func<T, MessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return _client.SubscribeAsync(subscribeMethod, configuration);
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			return _client.PublishAsync(message, globalMessageId, configuration);
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, MessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
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
