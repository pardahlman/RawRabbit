using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit.Compatibility.Legacy
{
	public class BaseBusClient<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IBusClient _busClient;

		public BaseBusClient(IBusClient busClient)
		{
			_busClient = busClient;
		}
		public Task<ISubscription> SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return _busClient.SubscribeAsync(subscribeMethod, configuration) as Task<ISubscription>;
		}

		public Task PublishAsync<T>(T message = default(T), Action<IPublishConfigurationBuilder> configuration = null)
		{
			return _busClient.PublishAsync(message, configuration);
		}

		public Task<ISubscription> RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Action<IRequestConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}
	}
}
