using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Serialization;
using RawRabbit.Context.Enhancer;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit.Operations
{
	public class Responder<TMessageContext> : IShutdown, IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IRawConsumerFactory _consumerFactory;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly IBasicPropertiesProvider _propertyProvider;
		private readonly IErrorHandlingStrategy _errorHandling;
		private readonly RawRabbitConfiguration _config;
		private readonly ILogger _logger = LogManager.GetLogger<Responder<TMessageContext>>();
		private readonly List<ISubscription> _subscriptions;

		public Responder(
			IChannelFactory channelFactory,
			ITopologyProvider topologyProvider,
			IRawConsumerFactory consumerFactory,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IContextEnhancer contextEnhancer,
			IBasicPropertiesProvider propertyProvider,
			IErrorHandlingStrategy errorHandling,
			RawRabbitConfiguration config)
		{
			_channelFactory = channelFactory;
			_topologyProvider = topologyProvider;
			_consumerFactory = consumerFactory;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
			_propertyProvider = propertyProvider;
			_errorHandling = errorHandling;
			_config = config;
			_subscriptions = new List<ISubscription>();
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			var routingKey = _config.RouteWithGlobalId ? $"{cfg.RoutingKey}.#" : cfg.RoutingKey;
			var topologyTask = _topologyProvider.BindQueueAsync(cfg.Queue, cfg.Exchange, routingKey);
			var channelTask = _channelFactory.CreateChannelAsync();

			var respondTask = Task.WhenAll(topologyTask, channelTask)
				.ContinueWith(t =>
				{
					if (topologyTask.IsFaulted)
					{
						throw topologyTask.Exception ?? new Exception("Topology Task Fauled");
					}
					var consumer = _consumerFactory.CreateConsumer(cfg, channelTask.Result);
					consumer.OnMessageAsync = (o, args) => _errorHandling.ExecuteAsync(() =>
					{
						var body = _serializer.Deserialize<TRequest>(args.Body);
						var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
						_contextEnhancer.WireUpContextFeatures(context, consumer, args);

						return onMessage(body, context)
							.ContinueWith(tResponse =>
							{
								if (tResponse.IsFaulted)
								{
									throw tResponse.Exception ?? new Exception();
								}
								if (consumer.AcknowledgedTags.Contains(args.DeliveryTag))
								{
									return;
								}
								if (tResponse.Result == null)
								{
									return;
								}
								_logger.LogDebug($"Sending response to request with correlation '{args.BasicProperties.CorrelationId}'.");
								consumer.Model.BasicPublish(
									exchange: string.Empty,
									routingKey: args.BasicProperties.ReplyTo,
									basicProperties: _propertyProvider.GetProperties<TResponse>(p => p.CorrelationId = args.BasicProperties.CorrelationId),
									body: _serializer.Serialize(tResponse.Result)
								);
							});
					}, exception => _errorHandling.OnResponseHandlerExceptionAsync(consumer, cfg, args, exception)); 
					consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
					return new Subscription(consumer, cfg.Queue.QueueName);
				});
			Task.WaitAll(respondTask);
			_subscriptions.Add(respondTask.Result);
			return respondTask.Result;
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			_logger.LogDebug("Shutting down Responder.");
			foreach (var subscription in _subscriptions)
			{
				subscription.Dispose();
			}
			graceful = graceful ?? _config.GracefulShutdown;
			await Task.Delay(graceful.Value);
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
