using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Legacy.Subscribe;
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
		private readonly ITopologyProvider _topologyProvider;
		private readonly IChannelFactory _channelFactory;
		private readonly ILogger _logger = LogManager.GetLogger<DefaultStrategy>();
		private readonly string _messageExceptionName = typeof(MessageHandlerException).Name;
		private readonly ExchangeDeclaration _errorExchangeCfg;

		public DefaultStrategy(IMessageSerializer serializer, INamingConventions conventions, IBasicPropertiesProvider propertiesProvider, ITopologyProvider topologyProvider, IChannelFactory channelFactory)
		{
			_serializer = serializer;
			_propertiesProvider = propertiesProvider;
			_topologyProvider = topologyProvider;
			_channelFactory = channelFactory;
			_errorExchangeCfg = ExchangeDeclaration.Default;
			_errorExchangeCfg.Name = conventions.ErrorExchangeNamingConvention();
		}

		public virtual Task OnResponseHandlerExceptionAsync(IRawConsumer rawConsumer, IConsumerConfiguration cfg, BasicDeliverEventArgs args, Exception exception)
		{
			_logger.LogError($"An unhandled exception was thrown in the response handler for message '{args.BasicProperties.MessageId}'.", exception);
			var innerException = UnwrapInnerException(exception);
			var exceptionInfo = new MessageHandlerExceptionInformation
			{
				Message = $"An unhandled exception was thrown when consuming a message\n  MessageId: {args.BasicProperties.MessageId}\n  Queue: '{cfg.Queue.FullQueueName}'\n  Exchange: '{cfg.Exchange.Name}'\nSee inner exception for more details.",
				ExceptionType = innerException.GetType().FullName,
				StackTrace = innerException.StackTrace,
				InnerMessage = innerException.Message
			};
			_logger.LogInformation($"Sending MessageHandlerException with CorrelationId '{args.BasicProperties.CorrelationId}'");
			rawConsumer.Model.BasicPublish(
				exchange: string.Empty,
				routingKey: args.BasicProperties?.ReplyTo ?? string.Empty,
				basicProperties: _propertiesProvider.GetProperties<MessageHandlerExceptionInformation>(p =>
				{
					p.CorrelationId = args.BasicProperties?.CorrelationId ?? string.Empty;
					p.Headers.Add(PropertyHeaders.ExceptionHeader, _messageExceptionName);
				}),
				body: _serializer.Serialize(exceptionInfo)
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
				var exceptionInfo = _serializer.Deserialize<MessageHandlerExceptionInformation>(args.Body);
				var exception = new MessageHandlerException(exceptionInfo.Message)
				{
					InnerExceptionType = exceptionInfo.ExceptionType,
					InnerStackTrace = exceptionInfo.StackTrace,
					InnerMessage = exceptionInfo.InnerMessage
				};
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

		public virtual async Task OnSubscriberExceptionAsync(IRawConsumer consumer, SubscriptionConfiguration config, BasicDeliverEventArgs args, Exception exception)
		{
			if (!config.NoAck)
			{
				consumer.Model.BasicAck(args.DeliveryTag, false);
				consumer.AcknowledgedTags.Add(args.DeliveryTag);
			}
			try
			{
				_logger.LogError($"Error thrown in Subscriber: ", exception);
				_logger.LogDebug($"Attempting to publish message '{args.BasicProperties.MessageId}' to error exchange.");

				await _topologyProvider.DeclareExchangeAsync(_errorExchangeCfg);
				var channel = await _channelFactory.GetChannelAsync();
				var msg = _serializer.Deserialize(args);
				var errorMsg = new HandlerExceptionMessage
				{
					Exception = exception,
					Time = DateTime.Now,
					Host = Environment.MachineName,
					Message = msg,
				};
				channel.BasicPublish(
					exchange: _errorExchangeCfg.Name,
					routingKey: args.RoutingKey,
					basicProperties: args.BasicProperties,
					body: _serializer.Serialize(errorMsg)
					);
				channel.Close();
			}
			catch (Exception e)
			{
				_logger.LogWarning($"Unable to publish message '{args.BasicProperties.MessageId}' to default error exchange.", e);
			}
		}
	}
}
