using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Common.Operations;
using RawRabbit.Core.Client;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Request;
using RawRabbit.Core.Configuration.Respond;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Context;
using RawRabbit.Core.Message;

namespace RawRabbit.Client
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

		public Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null) where T : MessageBase
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			return _subscriber.SubscribeAsync(subscribeMethod, config);
		}

		public Task PublishAsync<T>(T message = null, Action<IPublishConfigurationBuilder> configuration = null) where T : MessageBase
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			return _publisher.PublishAsync(message, config);
		}

		public Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null) where TRequest : MessageBase where TResponse : MessageBase
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
			return _responder.RespondAsync(onMessage, config);
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = null, Action<IRequestConfigurationBuilder> configuration = null) where TRequest : MessageBase where TResponse : MessageBase 
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
			return _requester.RequestAsync<TRequest, TResponse>(message, config);
		}
	}
}
