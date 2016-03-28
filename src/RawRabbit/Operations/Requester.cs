using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
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
	public class Requester<TMessageContext> : IDisposable, IShutdown, IRequester where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IErrorHandlingStrategy _errorStrategy;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly ITopologyProvider _topologyProvider;
		private readonly RawRabbitConfiguration _config;
		private readonly ConcurrentDictionary<string, ResponseCompletionSource> _responseDictionary;
		private readonly ConcurrentDictionary<IModel, ConsumerCompletionSource> _consumerCompletionSources;
		private readonly ILogger _logger = LogManager.GetLogger<Requester<TMessageContext>>();
		private ConsumerCompletionSource _currentConsumer;
		private readonly Task _completed = Task.FromResult(true);

		public Requester(
			IChannelFactory channelFactory,
			IConsumerFactory consumerFactory,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IErrorHandlingStrategy errorStrategy,
			IBasicPropertiesProvider propertiesProvider,
			ITopologyProvider topologyProvider,
			RawRabbitConfiguration config)
		{
			_channelFactory = channelFactory;
			_consumerFactory = consumerFactory;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_errorStrategy = errorStrategy;
			_propertiesProvider = propertiesProvider;
			_topologyProvider = topologyProvider;
			_config = config;
			_responseDictionary = new ConcurrentDictionary<string, ResponseCompletionSource>();
			_consumerCompletionSources = new ConcurrentDictionary<IModel, ConsumerCompletionSource>();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			if (!_topologyProvider.IsInitialized(cfg.Queue) || !_topologyProvider.IsInitialized(cfg.Exchange))
			{
				var queueTask = _topologyProvider.DeclareQueueAsync(cfg.Queue);
				var exchangeTask = _topologyProvider.DeclareExchangeAsync(cfg.Exchange);
				Task.WaitAll(queueTask, exchangeTask);
			}

			if (_currentConsumer != null && _currentConsumer.ConsumerQueues.ContainsKey(cfg.Queue.FullQueueName) && _currentConsumer.IsCompletedAndOpen())
			{
				return SendRequestAsync<TRequest, TResponse>(message, globalMessageId, cfg, _currentConsumer.Consumer);
			}

			return GetOrCreateConsumerAsync(cfg)
				.ContinueWith(tConsumer => SendRequestAsync<TRequest, TResponse>(message, globalMessageId, cfg, tConsumer.Result))
				.Unwrap();
		}

		private Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg, IRawConsumer consumer)
		{
			var correlationId = Guid.NewGuid().ToString();
			var responseSource = new ResponseCompletionSource
			{
				RequestTimer = new Timer(state =>
				{
					ResponseCompletionSource rcs;
					if (!_responseDictionary.TryRemove(correlationId, out rcs))
					{
						_logger.LogWarning($"Unable to find request timer for {correlationId}.");
						return;
					}
					rcs.RequestTimer?.Dispose();
					rcs.TrySetException(
						new TimeoutException($"The request '{correlationId}' timed out after {_config.RequestTimeout.ToString("g")}."));
				}, null, _config.RequestTimeout, new TimeSpan(-1))
			};

			_responseDictionary.TryAdd(correlationId, responseSource);

			consumer.Model.BasicPublish(
				exchange: cfg.Exchange.ExchangeName,
				routingKey: _config.RouteWithGlobalId ? $"{cfg.RoutingKey}.{globalMessageId}" : cfg.RoutingKey,
				basicProperties: _propertiesProvider.GetProperties<TResponse>(p =>
					{
						p.ReplyTo = cfg.ReplyQueue.QueueName;
						p.CorrelationId = correlationId;
						p.Expiration = _config.RequestTimeout.TotalMilliseconds.ToString();
						p.Headers.Add(PropertyHeaders.Context, _contextProvider.GetMessageContext(globalMessageId));
					}),
				body: _serializer.Serialize(message)
			);
			return responseSource.Task.ContinueWith(tResponse =>
			{
				if (tResponse.IsFaulted)
				{
					throw tResponse.Exception?.InnerException ?? new Exception("Failed to recieve response");
				}
				return (TResponse)tResponse.Result;
			});
		}

		private Task<IRawConsumer> GetOrCreateConsumerAsync(IConsumerConfiguration cfg)
		{
			return _channelFactory
				.GetChannelAsync()
				.ContinueWith(tChannel =>
					{
						var consumerCs = new ConsumerCompletionSource();
						if (_consumerCompletionSources.TryAdd(tChannel.Result, consumerCs))
						{
							var newConsumer = _consumerFactory.CreateConsumer(cfg, tChannel.Result);
							WireUpConsumer(newConsumer);
							consumerCs.ConsumerQueues.TryAdd(
								key: cfg.Queue.FullQueueName,
								value: newConsumer.Model.BasicConsume(cfg.Queue.FullQueueName, cfg.NoAck, newConsumer)
							);
							_currentConsumer = consumerCs;
							_logger.LogInformation($"Created consumer on channel '{tChannel.Result.ChannelNumber}' that consumes messages from '{cfg.Queue.FullQueueName}'.");
							consumerCs.TrySetResult(newConsumer);
							return consumerCs.Task;
						}
						_logger.LogDebug($"Consumer for channel '{tChannel.Result.ChannelNumber}' exists. Using it.");
						consumerCs = _consumerCompletionSources[tChannel.Result];
						if (consumerCs.ConsumerQueues.ContainsKey(cfg.Queue.FullQueueName))
						{
							return consumerCs.Task;
						}
						return consumerCs.Task.ContinueWith(t =>
						{
							lock (consumerCs.Consumer)
							{
								if (consumerCs.ConsumerQueues.ContainsKey(cfg.Queue.FullQueueName))
								{
									return t.Result;
								}
								consumerCs.ConsumerQueues.TryAdd(
									key: cfg.Queue.FullQueueName,
									value: consumerCs.Consumer.Model.BasicConsume(cfg.Queue.FullQueueName, cfg.NoAck, consumerCs.Consumer)
								);
								_logger.LogDebug($"Existign consumer for channel '{tChannel.Result.ChannelNumber}' consumes '{cfg.Queue.FullQueueName}'.");
								return t.Result;
							}
						});
					})
				.Unwrap();
		}

		private void WireUpConsumer(IRawConsumer consumer)
		{
			consumer.OnMessageAsync = (o, args) =>
			{
				ResponseCompletionSource responseTcs;
				if (_responseDictionary.TryRemove(args.BasicProperties.CorrelationId, out responseTcs))
				{
					_logger.LogDebug($"Recived response with correlationId {args.BasicProperties.CorrelationId}.");
					responseTcs.RequestTimer.Dispose();

					_errorStrategy.OnResponseRecievedAsync(args, responseTcs);
					if (responseTcs.Task.IsFaulted)
					{
						return _completed;
					}
					var response = _serializer.Deserialize(args);
					responseTcs.TrySetResult(response);
					return _completed;
				}
				_logger.LogWarning($"Unable to find callback for {args.BasicProperties.CorrelationId}.");
				return _completed;
			};
		}

		private class ResponseCompletionSource : TaskCompletionSource<object>
		{
			public Timer RequestTimer { get; set; }
		}

		private class ConsumerCompletionSource : TaskCompletionSource<IRawConsumer>
		{
			public ConcurrentDictionary<string, string> ConsumerQueues { get; }
			public IRawConsumer Consumer => Task.IsCompleted ? Task.Result : null;

			public ConsumerCompletionSource()
			{
				ConsumerQueues = new ConcurrentDictionary<string, string>();
			}

			public bool IsCompletedAndOpen()
			{
				return Task.IsCompleted && Task.Result.Model.IsOpen;
			}
		}

		public void Dispose()
		{
			(_channelFactory as IDisposable)?.Dispose();
		}

		public async Task ShutdownAsync()
		{
			_logger.LogDebug("Shutting down Requester.");
			foreach (var ccS in _consumerCompletionSources)
			{
				if (!ccS.Value.IsCompletedAndOpen())
				{
					continue;
				}
				ccS.Value.Consumer.Disconnect();
			}
			if (!_responseDictionary.IsEmpty)
			{
				await Task.Delay(_config.RequestTimeout);
			}
			Dispose();
		}
	}
}
