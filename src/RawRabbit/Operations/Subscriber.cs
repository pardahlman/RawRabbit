using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
using RawRabbit.Context.Provider;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Subscriber<TMessageContext> : IShutdown, ISubscriber<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly IErrorHandlingStrategy _errorHandling;
		private readonly RawRabbitConfiguration _config;
		private readonly List<ISubscription> _subscriptions;
		private readonly ILogger _logger = LogManager.GetLogger<Subscriber<TMessageContext>>();

		public Subscriber(
			IChannelFactory channelFactory,
			IConsumerFactory consumerFactory,
			ITopologyProvider topologyProvider,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IContextEnhancer contextEnhancer,
			IErrorHandlingStrategy errorHandling,
			RawRabbitConfiguration config)
		{
			_channelFactory = channelFactory;
			_consumerFactory = consumerFactory;
			_topologyProvider = topologyProvider;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
			_errorHandling = errorHandling;
			_config = config;
			_subscriptions = new List<ISubscription>();
		}

		public ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config)
		{
			var routingKey = _config.RouteWithGlobalId
				? $"{config.RoutingKey}.#"
				: config.RoutingKey;

			var topologyTask = _topologyProvider.BindQueueAsync(config.Queue, config.Exchange, routingKey);
			var channelTask = _channelFactory.CreateChannelAsync();

			var subscriberTask = Task
				.WhenAll(topologyTask, channelTask)
				.ContinueWith(t =>
				{
					var consumer = _consumerFactory.CreateConsumer(config, channelTask.Result);
					consumer.OnMessageAsync = (o, args) => _errorHandling.ExecuteAsync(() =>
					{
						var body = _serializer.Deserialize<T>(args.Body);
						var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
						_contextEnhancer.WireUpContextFeatures(context, consumer, args);
						return subscribeMethod(body, context);
					}, exception => _errorHandling.OnSubscriberExceptionAsync(consumer, config, args, exception));
					consumer.Model.BasicConsume(config.Queue.FullQueueName, config.NoAck, consumer);
					_logger.LogDebug($"Setting up a consumer on channel '{channelTask.Result.ChannelNumber}' for queue {config.Queue.QueueName} with NoAck set to {config.NoAck}.");
					return new Subscription(consumer, config.Queue.FullQueueName);
				});
			Task.WaitAll(subscriberTask);
			_subscriptions.Add(subscriberTask.Result);
			return subscriberTask.Result;
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			_logger.LogDebug("Shutting down Subscriber.");
			foreach (var subscription in _subscriptions.Where(s => s.Active))
			{
				subscription.Dispose();
			}
			graceful = graceful ?? _config.GracefulShutdown;
			await Task.Delay(graceful.Value);
			(_consumerFactory as IDisposable)?.Dispose();
			(_channelFactory as IDisposable)?.Dispose();
			(_topologyProvider as IDisposable)?.Dispose();
		}
	}
}
