using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Requester<TMessageContext> : IRequester where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IErrorHandlingStrategy _errorStrategy;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly ITopologyProvider _topologyProvider;
		private readonly TimeSpan _requestTimeout;
		private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _responseTcsDictionary;
		private readonly ConcurrentDictionary<string, Timer> _requestTimerDictionary;
		private readonly ConcurrentDictionary<IModel, IRawConsumer> _channelToConsumer;
		private readonly ConcurrentDictionary<IRawConsumer, List<string>> _consumerToQueue;
		private readonly ILogger _logger = LogManager.GetLogger<Requester<TMessageContext>>();
		private readonly object _topologyLock = new object();
		private readonly object _consumerLock = new object();

		public Requester(
			IChannelFactory channelFactory,
			IConsumerFactory consumerFactory,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IErrorHandlingStrategy errorStrategy,
			IBasicPropertiesProvider propertiesProvider,
			ITopologyProvider topologyProvider,
			TimeSpan requestTimeout)
		{
			_channelFactory = channelFactory;
			_consumerFactory = consumerFactory;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_errorStrategy = errorStrategy;
			_propertiesProvider = propertiesProvider;
			_topologyProvider = topologyProvider;
			_requestTimeout = requestTimeout;
			_responseTcsDictionary = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
			_requestTimerDictionary = new ConcurrentDictionary<string, Timer>();
			_channelToConsumer = new ConcurrentDictionary<IModel, IRawConsumer>();
			_consumerToQueue = new ConcurrentDictionary<IRawConsumer, List<string>>();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			var queueTask = _topologyProvider.DeclareQueueAsync(cfg.Queue);
			var exchangeTask = _topologyProvider.DeclareExchangeAsync(cfg.Exchange);
			var consumerTask = GetOrCreateConsumerAsync(cfg);

			return Task
				.WhenAll(consumerTask, queueTask, exchangeTask)
				.ContinueWith(t =>
				{
					var consumer = consumerTask.Result;
					lock (consumer)
					{
						if (!_consumerToQueue[consumer].Contains(cfg.Queue.QueueName))
						{
							consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
							_consumerToQueue[consumer].Add(cfg.Queue.QueueName);
						}
					}
					
					var props = _propertiesProvider.GetProperties<TResponse>(p =>
					{
						p.ReplyTo = cfg.ReplyQueue.QueueName;
						p.CorrelationId = Guid.NewGuid().ToString();
						p.Expiration = _requestTimeout.TotalMilliseconds.ToString();
						p.Headers.Add(PropertyHeaders.Context, _contextProvider.GetMessageContext(globalMessageId));
					});
					var body = _serializer.Serialize(message);
					var responseTcs = new TaskCompletionSource<object>();
					_responseTcsDictionary.TryAdd(props.CorrelationId, responseTcs);

					_requestTimerDictionary.TryAdd(
						props.CorrelationId,
						new Timer(state =>
						{
							Timer timer;
							if (!_requestTimerDictionary.TryRemove(props.CorrelationId, out timer))
							{
								_logger.LogWarning($"Unable to find request timer for {props.CorrelationId}.");
							}
							timer?.Dispose();
							responseTcs.TrySetException(new TimeoutException($"The request '{props.CorrelationId}' timed out after {_requestTimeout.ToString("g")}."));
						}, null, _requestTimeout, new TimeSpan(-1)));

					consumer.Model.BasicPublish(
						exchange: cfg.Exchange.ExchangeName,
						routingKey: cfg.RoutingKey,
						basicProperties: props,
						body: body
					);
					return responseTcs.Task.ContinueWith(tResponse => (TResponse) tResponse.Result);
				})
				.Unwrap();
		}

		private Task<IRawConsumer> GetOrCreateConsumerAsync(IConsumerConfiguration cfg)
		{
			return _channelFactory
				.GetChannelAsync()
				.ContinueWith(tChannel =>
				{
					IRawConsumer existingConsumer;
					if (_channelToConsumer.TryGetValue(tChannel.Result, out existingConsumer))
					{
						return existingConsumer;
					}
					lock (_consumerLock)
					{
						if (_channelToConsumer.TryGetValue(tChannel.Result, out existingConsumer))
						{
							return existingConsumer;
						}
						var newConsumer = _consumerFactory.CreateConsumer(cfg, tChannel.Result);
						WireUpConsumer(newConsumer);
						_channelToConsumer.TryAdd(tChannel.Result, newConsumer);
						_consumerToQueue.TryAdd(newConsumer, new List<string>());
						return newConsumer;
					}
				});
		}

		private void WireUpConsumer(IRawConsumer consumer)
		{
			consumer.OnMessageAsync = (o, args) =>
			{
				TaskCompletionSource<object> responseTcs;
				if (_responseTcsDictionary.TryRemove(args.BasicProperties.CorrelationId, out responseTcs))
				{
					_logger.LogDebug($"Recived response with correlationId {args.BasicProperties.CorrelationId}.");

					Timer timer;
					if (_requestTimerDictionary.TryRemove(args.BasicProperties.CorrelationId, out timer))
					{
						timer?.Dispose();
					}
					else
					{
						_logger.LogInformation($"Unable to find request timer for message {args.BasicProperties.CorrelationId}.");
					}
					_errorStrategy.OnResponseRecievedAsync(args, responseTcs);
					if (responseTcs?.Task?.IsFaulted ?? true)
					{
						return Task.FromResult(true);
					}
					var response = _serializer.Deserialize(args);
					responseTcs.TrySetResult(response);
					return Task.FromResult(true);
				}
				_logger.LogWarning($"Unable to find callback for {args.BasicProperties.CorrelationId}.");
				throw new Exception($"Can not find callback for {args.BasicProperties.CorrelationId}");
			};
		}
	}
}
