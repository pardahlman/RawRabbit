using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Operations;

namespace RawRabbit.Common
{
	public abstract class BusClientBase<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : MessageContext
	{
		private readonly IConfigurationEvaluator _configEval;
		private readonly ISubscriber<TMessageContext> _subscriber;
		private readonly IPublisher _publisher;
		private readonly IResponder<TMessageContext> _responder;
		private readonly IRequester _requester;

		protected BusClientBase(
			IConfigurationEvaluator configEval,
			ISubscriber<TMessageContext> subscriber,
			IPublisher publisher,
			IResponder<TMessageContext> responder,
			IRequester requester)
		{
			_configEval = configEval;
			_subscriber = subscriber;
			_publisher = publisher;
			_responder = responder;
			_requester = requester;
		}

		public Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			return _subscriber.SubscribeAsync(subscribeMethod, config);
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			return _publisher.PublishAsync(message, globalMessageId, config);
		}

		public Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
			return _responder.RespondAsync(onMessage, config);
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(), Action<IRequestConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
			return _requester.RequestAsync<TRequest, TResponse>(message, globalMessageId, config);
		}
	}
}
