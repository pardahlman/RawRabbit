using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Exceptions;
using RawRabbit.Serialization;

namespace RawRabbit.ErrorHandling
{
	public class DefaultStrategy : IErrorHandlingStrategy
	{
		private readonly IMessageSerializer _serializer;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly string _messageExceptionName = typeof(MessageHandlerException).Name;

		public DefaultStrategy(IMessageSerializer serializer, IBasicPropertiesProvider propertiesProvider)
		{
			_serializer = serializer;
			_propertiesProvider = propertiesProvider;
		}

		public Task OnRequestHandlerExceptionAsync(IRawConsumer rawConsumer, IConsumerConfiguration cfg, BasicDeliverEventArgs args, Exception exception)
		{
			var innerException = UnwrapInnerException(exception);
			var rawException = new MessageHandlerException(
				message: $"An unhandled exception was thrown when consuming a message\n  MessageId: {args.BasicProperties.MessageId}\n  Queue: '{cfg.Queue.FullQueueName}'\n  Exchange: '{cfg.Exchange.ExchangeName}'\nSee inner exception for more details.",
				inner: innerException
			);

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

		public Task OnResponseRecievedAsync<TResponse>(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs)
		{
			OnResponseRecieved<TResponse>(args, responseTcs);
			return Task.FromResult(true);
		}

		public void OnResponseRecieved<TResponse>(BasicDeliverEventArgs args, TaskCompletionSource<object> responseTcs)
		{
			var containsException = args?.BasicProperties?.Headers?.ContainsKey(PropertyHeaders.ExceptionHeader) ?? false;

			if (!containsException)
			{
				return;
			}

			var exception = _serializer.Deserialize<MessageHandlerException>(args.Body);
			responseTcs.TrySetException(exception);
		}
	}
}
