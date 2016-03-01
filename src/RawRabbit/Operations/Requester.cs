using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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
	public class Requester<TMessageContext> : OperatorBase, IRequester where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IErrorHandlingStrategy _errorStrategy;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly TimeSpan _requestTimeout;
		private readonly ConcurrentDictionary<Type, IRawConsumer> _typeToConsumer;
		private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _responseTcsDictionary;
		private readonly ConcurrentDictionary<string, Timer> _requestTimerDictionary;
		private Timer _disposeConsumerTimer;
		private readonly ILogger _logger = LogManager.GetLogger<Requester<TMessageContext>>();
		private bool _channelActive;

		public Requester(
			IChannelFactory channelFactory,
			IConsumerFactory consumerFactory,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IErrorHandlingStrategy errorStrategy,
			IBasicPropertiesProvider propertiesProvider,
			TimeSpan requestTimeout)
				: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_errorStrategy = errorStrategy;
			_propertiesProvider = propertiesProvider;
			_requestTimeout = requestTimeout;
			_typeToConsumer = new ConcurrentDictionary<Type, IRawConsumer>();
			_responseTcsDictionary = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
			_requestTimerDictionary = new ConcurrentDictionary<string, Timer>();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			var props = _propertiesProvider.GetProperties<TResponse>(p =>
			{
				p.ReplyTo = cfg.ReplyQueue.QueueName;
				p.CorrelationId = Guid.NewGuid().ToString();
				p.Expiration = _requestTimeout.TotalMilliseconds.ToString();
				p.Headers.Add(PropertyHeaders.Context, _contextProvider.GetMessageContext(globalMessageId));
			});
			var consumer = GetOrCreateConsumerForType<TResponse>(cfg);
			var body = Serializer.Serialize(message);

			Task.Run(() => CreateOrUpdateDisposeTimer());

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
		}

		private void CreateOrUpdateDisposeTimer()
		{
			if (_disposeConsumerTimer != null)
			{
				_channelActive = true;
				return;
			}
			_disposeConsumerTimer = new Timer(state =>
			{
				if (_channelActive)
				{
					_channelActive = false;
					return;
				}
				if (!_responseTcsDictionary.IsEmpty)
				{
					return;
				}
				_disposeConsumerTimer?.Dispose();
				_disposeConsumerTimer = null;
				foreach (var type in _typeToConsumer.Keys)
				{
					IRawConsumer consumer;
					if (_typeToConsumer.TryRemove(type, out consumer))
					{
						consumer?.Disconnect();
						consumer?.Model?.Dispose();
					}
				}
			}, null, _requestTimeout, _requestTimeout);
		}

		private IRawConsumer GetOrCreateConsumerForType<TResponse>(IConsumerConfiguration cfg)
		{
			var responseType = typeof(TResponse);
			IRawConsumer existingConsumer;
			if (_typeToConsumer.TryGetValue(responseType, out existingConsumer))
			{
				_logger.LogDebug($"Channel for existing cunsomer of {responseType.Name} found.");
				if (existingConsumer.Model.IsOpen)
				{
					_logger.LogDebug($"Channel is open and will be reused.");
					return existingConsumer;
				}
				else
				{
					existingConsumer?.Model?.Dispose();
					_logger.LogInformation($"Channel for consumer of {responseType.Name} is closed. A new consumer will be created, of course.");
				}
			}

			_logger.LogInformation($"Creatinga new consumer for message {responseType.Name}.");
			var consumer = _consumerFactory.CreateConsumer(cfg, ChannelFactory.CreateChannel());
			_typeToConsumer.TryAdd(typeof(TResponse), consumer);

			DeclareQueue(cfg.Queue, consumer.Model);
			DeclareExchange(cfg.Exchange, consumer.Model);
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
					_errorStrategy.OnResponseRecievedAsync<TResponse>(args, responseTcs);
					if (responseTcs?.Task?.IsFaulted ?? true)
					{
						return Task.FromResult(true);
					}
					var response = Serializer.Deserialize(args);
					responseTcs.TrySetResult(response);
					return Task.FromResult(true);
				}
				_logger.LogWarning($"Unable to find callback for {args.BasicProperties.CorrelationId}.");
				throw new Exception($"Can not find callback for {args.BasicProperties.CorrelationId}");
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
			return consumer;
		}
	}
}
