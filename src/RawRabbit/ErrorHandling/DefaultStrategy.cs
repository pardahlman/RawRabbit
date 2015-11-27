using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Consumer.Contract;
using RawRabbit.Exceptions;
using RawRabbit.Serialization;

namespace RawRabbit.ErrorHandling
{
	public class DefaultStrategy : IErrorHandlingStrategy
	{
		private readonly IMessageSerializer _serializer;
		private const string ExceptionHeader = "exception";
		private readonly string _messageExceptionName = typeof (MessageHandlerException).Name;

		public DefaultStrategy(IMessageSerializer serializer)
		{
			_serializer = serializer;
		}

		public Task OnRequestHandlerExceptionAsync(IRawConsumer rawConsumer, BasicDeliverEventArgs args, Exception exception)
		{
			var innerException = UnwrapInnerException(exception);
			var rawException = new MessageHandlerException(
				message: $"An unhandled exception was thrown when responding to message with id {args.BasicProperties.MessageId}. See inner exception for more details.",
				inner:innerException
			);
			
			rawConsumer.Model.BasicPublish(
				exchange: string.Empty,
				routingKey: args.BasicProperties.ReplyTo,
				basicProperties: new BasicProperties
				{
					CorrelationId = args.BasicProperties.CorrelationId,
					Headers = new Dictionary<string, object>
					{
						{ExceptionHeader, _messageExceptionName }
					}
				},
				body: _serializer.Serialize(rawException)
			);
			rawConsumer.Model.BasicNack(args.DeliveryTag, false, false);
			return Task.FromResult(true);
		}

		private static Exception UnwrapInnerException(Exception exception)
		{
			if (exception is AggregateException)
			{
				return UnwrapInnerException(exception.InnerException);
			}
			return exception;
		}

		public Task OnResponseRecievedAsync<TResponse>(BasicDeliverEventArgs args, TaskCompletionSource<TResponse> responseTcs)
		{
			var containsException = args?.BasicProperties?.Headers?.ContainsKey(ExceptionHeader) ?? false;

			if (!containsException)
			{
				return Task.FromResult(true);
			}

			var exception = _serializer.Deserialize<MessageHandlerException>(args.Body);
			responseTcs.TrySetException(exception);
			
			return Task.FromResult(true);
		}
	}
}
