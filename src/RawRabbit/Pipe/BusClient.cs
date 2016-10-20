using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Pipe.Client;

namespace RawRabbit.Pipe
{
	public class BusClient<TContext> : IBusClient<TContext> where TContext : IMessageContext
	{
		private readonly Client.IBusClient _nextGeneration;
		private readonly IResourceDisposer _resourceDisposer;

		public BusClient(Client.IBusClient nextGeneration, IResourceDisposer resourceDisposer)
		{
			_nextGeneration = nextGeneration;
			_resourceDisposer = resourceDisposer;
		}

		public ISubscription SubscribeAsync<T>(Func<T, TContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			_nextGeneration.SubscribeAsync(subscribeMethod, configuration);
			return null;
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			return _nextGeneration.PublishAsync(message, configuration);
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(),
			Action<IRequestConfigurationBuilder> configuration = null)
		{
			throw new NotImplementedException();
		}

		public Task ShutdownAsync(TimeSpan? graceful = null)
		{
			_resourceDisposer.Dispose();
			return Task.FromResult(0);
		}
	}

	public class BusClient : BusClient<MessageContext>, IBusClient
	{
		public BusClient(Client.IBusClient nextGeneration, IResourceDisposer resourceDisposer) : base(nextGeneration, resourceDisposer)
		{
		}
	}
}
