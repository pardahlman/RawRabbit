using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit
{
	public interface IBusClient<out TMessageContext> : IShutdown where TMessageContext : IMessageContext
	{
		ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null);

		Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null);

		ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null);

		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(), Action<IRequestConfigurationBuilder> configuration = null);
	}
}
