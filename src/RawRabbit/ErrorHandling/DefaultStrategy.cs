using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.ErrorHandling
{
	public class DefaultStrategy : IErrorHandlingStrategy
	{
		private readonly IMessageSerializer _serializer;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly ILogger _logger = LogManager.GetLogger<DefaultStrategy>();
		private readonly string _messageExceptionName = typeof(MessageHandlerException).Name;

		public DefaultStrategy(IMessageSerializer serializer, IBasicPropertiesProvider propertiesProvider)
		{
			_serializer = serializer;
			_propertiesProvider = propertiesProvider;
		}

		public virtual Task OnResponseHandlerExceptionAsync(IRawConsumer rawConsumer, IConsumerConfiguration cfg, BasicDeliverEventArgs args, Exception exception)
		{
			_logger.LogError($"An unhandled exception was thrown in the response handler for message '{args.BasicProperties.MessageId}'.", exception);
			var innerException = UnwrapInnerException(exception);
			var rawException = new MessageHandlerException(
				message: $"An unhandled exception was thrown when consuming a message\n  MessageId: {args.BasicProperties.MessageId}\n  Queue: '{cfg.Queue.FullQueueName}'\n  Exchange: '{cfg.Exchange.ExchangeName}'\nSee inner exception for more details.",
				inner: innerException
			);

			_logger.LogInformation($"Sending MessageHandlerException with CorrelationId '{args.BasicProperties.CorrelationId}'");
			rawConsumer.Model.BasicPublish(
				exchange: string.Empty,
				routingKey: args.BasicProperties?.ReplyTo ?? string.Empty,
				basicProperties: _propertiesProvider.GetProperties<MessageHandlerException>(p =>
				{
					p.CorrelationId = args.BasicProperties?.CorrelationId ?? string.Empty;
					p.Headers.Add(PropertyHeaders.ExceptionHeader, _messageExceptionName);
				}),
				body: _serializer.Serialize(rawException)
			);

			if (!cfg.NoAck)
			{
				_logger.LogDebug($"Nack'ing message with delivery tag '{args.DeliveryTag}'.");
				rawConsumer.Model.BasicNack(args.DeliveryTag, false, false);
			}
			return Task.FromResult(true);
		}

		private static Exception UnwrapInnerException(Exception exception)
		{
			if (exception is AggregateException && exception.InnerException != null)
			{
				return UnwrapInnerException(exception.InnerException);
			}
			return exception;
		}

		public virtual Task OnResponseRecievedAsync(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs)
		{
			var containsException = args?.BasicProperties?.Headers?.ContainsKey(PropertyHeaders.ExceptionHeader) ?? false;

			if (containsException)
			{
				_logger.LogInformation($"Message '{args.BasicProperties.MessageId}' withh CorrelationId '{args.BasicProperties.CorrelationId}' contains exception. Deserialize and re-throw.");
				var exception = _serializer.Deserialize<MessageHandlerException>(args.Body);
				responseTcs.TrySetException(exception);
			}

			return Task.FromResult(true);
		}

		public virtual Task OnResponseRecievedException(IRawConsumer rawConsumer, IConsumerConfiguration cfg, BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs, Exception exception)
		{
			_logger.LogError($"An exception was thrown when recieving response to messaeg '{args.BasicProperties.MessageId}' with CorrelationId '{args.BasicProperties.CorrelationId}'.", exception);
			responseTcs.TrySetException(exception);
			return Task.FromResult(true);
		}

		public virtual Task ExecuteAsync(Func<Task> messageHandler, Func<Exception, Task> errorHandler)
		{
			try
			{
				return messageHandler()
					.ContinueWith(tHandler => tHandler.IsFaulted
							? errorHandler(tHandler.Exception)
							: tHandler
						);
			}
			catch (Exception e)
			{
				return errorHandler(e);
			}
		}

		public virtual Task OnSubscriberExceptionAsync(IRawConsumer consumer, SubscriptionConfiguration config, BasicDeliverEventArgs args, Exception exception)
		{
			if (!config.NoAck)
			{
				consumer.Model.BasicAck(args.DeliveryTag, false);
			}
			return Task.FromResult(true);
		}
	}
}
