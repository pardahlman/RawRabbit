using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.ErrorHandling;
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class SingleConsumerRequester<TMessageContext> : OperatorBase, IRequester where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IErrorHandlingStrategy _errorStrategy;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly TimeSpan _requestTimeout;
		private readonly ConcurrentDictionary<Type, IRawConsumer> _typeToConsumer;
		private readonly ConcurrentDictionary<string, object> _responseTcsDictionary;
		private readonly ConcurrentDictionary<string, Timer> _requestTimerDictionary;
		private Timer _disposeConsumerTimer;
		private readonly ILogger _logger = LogManager.GetLogger<SingleConsumerRequester<TMessageContext>>();

		public SingleConsumerRequester(
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
			_responseTcsDictionary = new ConcurrentDictionary<string, object>();
			_requestTimerDictionary = new ConcurrentDictionary<string, Timer>();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			var props = _propertiesProvider.GetProperties<TResponse>(p =>
			{
				p.ReplyTo = cfg.ReplyQueue.QueueName;
				p.CorrelationId = Guid.NewGuid().ToString();
				p.Expiration = _requestTimeout.TotalMilliseconds.ToString();
				p.Headers.Add(_contextProvider.ContextHeaderName, _contextProvider.GetMessageContext(globalMessageId));
			});
			var consumer = GetOrCreateConsumerForType<TResponse>(cfg);
			var body = Serializer.Serialize(message);

			Task.Run(() => CreateOrUpdateDisposeTimer());

			var responseTcs = new TaskCompletionSource<TResponse>();
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
					responseTcs.TrySetException(new TimeoutException($"The request timed out after {_requestTimeout.ToString("g")}."));
				}, null, _requestTimeout, new TimeSpan(-1)));

			consumer.Model.BasicPublish(
				exchange: cfg.Exchange.ExchangeName,
				routingKey: cfg.RoutingKey,
				basicProperties: props,
				body: body
			);
			return responseTcs.Task;
		}

		private void CreateOrUpdateDisposeTimer()
		{
			if (_disposeConsumerTimer != null)
			{
				return;
			}
			_disposeConsumerTimer = new Timer(state =>
			{
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
			if (_typeToConsumer.ContainsKey(responseType))
			{
				_logger.LogDebug($"Channel for existing cunsomer of {responseType.Name} found.");
				if (_typeToConsumer[responseType].Model.IsOpen)
				{
					_logger.LogDebug($"Channel is open and will be reused.");
					return _typeToConsumer[responseType];
				}
				else
				{
					_typeToConsumer[responseType]?.Model?.Dispose();
					_logger.LogInformation($"Channel for consumer of {responseType.Name} is closed. A new consumer will be created.");
				}
			}

			var consumer = _consumerFactory.CreateConsumer(cfg, ChannelFactory.CreateChannel());
			_typeToConsumer.TryAdd(typeof(TResponse), consumer);

			DeclareQueue(cfg.Queue, consumer.Model);
			DeclareExchange(cfg.Exchange, consumer.Model);
			consumer.OnMessageAsync = (o, args) =>
			{
				object tcsAsObj;
				if (_responseTcsDictionary.TryRemove(args.BasicProperties.CorrelationId, out tcsAsObj))
				{
					var tcs = tcsAsObj as TaskCompletionSource<TResponse>;
					_logger.LogDebug($"Recived response with correlationId {args.BasicProperties.CorrelationId}.");
					_requestTimerDictionary[args.BasicProperties.CorrelationId]?.Dispose();
					_errorStrategy.OnResponseRecievedAsync(args, tcs);
					if (tcs?.Task?.IsFaulted ?? true)
					{
						return Task.FromResult(true);
					}
					var response = Serializer.Deserialize<TResponse>(args.Body);
					tcs.TrySetResult(response);
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
