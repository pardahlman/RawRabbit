using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Request;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Requester<TMessageContext> : OperatorBase, IRequester where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly TimeSpan _requestTimeout;
		private readonly ILogger _logger = LogManager.GetLogger<Requester<TMessageContext>>();

		public Requester(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, TimeSpan requestTimeout) : base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_requestTimeout = requestTimeout;
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config)
		{
			return SendRequestAsync<TRequest, TResponse>(message, globalMessageId, config);
		}

		private Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration cfg)
		{
			var responseTcs = new TaskCompletionSource<TResponse>();
			var propsTask = GetRequestPropsAsync(cfg.ReplyQueue.QueueName, globalMessageId);
			var bodyTask = Task.Run(() => Serializer.Serialize(message));

			Task
				.WhenAll(propsTask, bodyTask)
				.ContinueWith(task =>
				{
					var channel = ChannelFactory.CreateChannel();
					DeclareQueue(cfg.Queue, channel);
					DeclareExchange(cfg.Exchange, channel);
					var consumer = _consumerFactory.CreateConsumer(cfg, channel);

					Timer requestTimeOutTimer = null;
					requestTimeOutTimer = new Timer(state =>
					{
						requestTimeOutTimer?.Dispose();
						channel.Dispose();
						responseTcs.TrySetException(new TimeoutException($"The request timed out after {_requestTimeout.ToString("g")}."));
					}, null, _requestTimeout, TimeSpan.FromMilliseconds(-1));

					consumer.OnMessageAsync = (o, args) =>
					{
						if (args.BasicProperties.CorrelationId != propsTask.Result.CorrelationId)
						{
							return Task.FromResult(false);
						}
						requestTimeOutTimer?.Dispose();
						return Task
							.Run(() => Serializer.Deserialize<TResponse>(args.Body))
							.ContinueWith(t =>
							{
								channel.Dispose();
								responseTcs.SetResult(t.Result);
							});
					};
					consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
					consumer.Model.BasicPublish(
							exchange: cfg.Exchange.ExchangeName,
							routingKey: cfg.RoutingKey,
							basicProperties: propsTask.Result,
							body: bodyTask.Result
						);
				});
			return responseTcs.Task;
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

		public override void Dispose()
		{
			_logger.LogDebug("Disposing requester.");
			base.Dispose();
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
