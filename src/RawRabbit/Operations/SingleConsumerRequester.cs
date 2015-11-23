using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
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
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class SingleConsumerRequester<TMessageContext> : OperatorBase, IRequester where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
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
			TimeSpan requestTimeout)
				: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_requestTimeout = requestTimeout;
			_typeToConsumer = new ConcurrentDictionary<Type, IRawConsumer>();
			_responseTcsDictionary = new ConcurrentDictionary<string, object>();
			_requestTimerDictionary = new ConcurrentDictionary<string, Timer>();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			var consumer = GetOrCreateConsumerForType<TResponse>(cfg);
			var propsTask = GetRequestPropsAsync(cfg.ReplyQueue.QueueName, globalMessageId);
			var bodyTask = Task.Run(() => Serializer.Serialize(message));
			var disposerTask = Task.Run(() => CreateOrUpdateDisposeTimer());

			return Task
				.WhenAll(propsTask, bodyTask, disposerTask)
				.ContinueWith(t =>
					{
						var props = propsTask.Result;
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
							basicProperties: propsTask.Result,
							body: bodyTask.Result
						);
						return responseTcs.Task;
					})
				.Unwrap();
		}

		private void CreateOrUpdateDisposeTimer()
		{
			if (_disposeConsumerTimer == null)
			{
				_disposeConsumerTimer = new Timer(state =>
				{
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
				}, null, _requestTimeout.Add(TimeSpan.FromSeconds(1)), new TimeSpan(-1));
			}
			else
			{
				_disposeConsumerTimer.Change(_requestTimeout.Add(TimeSpan.FromSeconds(1)), new TimeSpan(-1));
			}
		}

		private IRawConsumer GetOrCreateConsumerForType<TResponse>(IConsumerConfiguration cfg)
		{
			var responseType = typeof (TResponse);
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
				object tcs;
				if (_responseTcsDictionary.TryRemove(args.BasicProperties.CorrelationId, out tcs))
				{
					_logger.LogDebug($"Recived response with correlationId {args.BasicProperties.CorrelationId}.");
					_requestTimerDictionary[args.BasicProperties.CorrelationId]?.Dispose();
					return Task
						.Run(() => Serializer.Deserialize<TResponse>(args.Body))
						.ContinueWith(t =>
						{
							(tcs as TaskCompletionSource<TResponse>)?.TrySetResult(t.Result);
						});
				}
				throw new Exception($"Can not find callback for {args.BasicProperties.CorrelationId}");
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
			return consumer;
		}

		private Task<IBasicProperties> GetRequestPropsAsync(string queueName, Guid globalMessageId)
		{
			return Task
				.Run(() => _contextProvider.GetMessageContextAsync(globalMessageId))
				.ContinueWith(ctxTask =>
				{
					IBasicProperties props = new BasicProperties
					{
						ReplyTo = queueName,
						CorrelationId = Guid.NewGuid().ToString(),
						Expiration = _requestTimeout.TotalMilliseconds.ToString(),
						MessageId = Guid.NewGuid().ToString(),
						Headers = new Dictionary<string, object>
						{
							{_contextProvider.ContextHeaderName, ctxTask.Result}
						}
					};
					return props;
				});
		}
	}
}
