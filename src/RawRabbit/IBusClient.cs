using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;

namespace RawRabbit
{
	public interface IBusClient<out TMessageContext> where TMessageContext : MessageContext
	{
		Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
			where T : MessageBase;

		Task PublishAsync<T>(T message = null, Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
			where T : MessageBase;

		Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
			where TRequest : MessageBase
			where TResponse : MessageBase;

		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = null, Guid globalMessageId = new Guid(), Action<IRequestConfigurationBuilder> configuration = null)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}
}
