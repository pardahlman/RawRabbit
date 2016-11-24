using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Legacy.Publish;
using RawRabbit.Configuration.Legacy.Request;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Legacy.Subscribe;
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
			throw new NotImplementedException();
		}

		public Task PublishAsync<T>(T message = default(T), Action<IPublishConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
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
