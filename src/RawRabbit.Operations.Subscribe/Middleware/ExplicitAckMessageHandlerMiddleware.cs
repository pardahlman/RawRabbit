using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class ExplicitAckMessageHandlerMiddleware : Pipe.Middleware.Middleware
	{
		private readonly INamingConventions _conventions;

		public ExplicitAckMessageHandlerMiddleware(INamingConventions conventions)
		{
			_conventions = conventions;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message)
				.ContinueWith(t =>
				{
					var ack = (t as Task<Acknowledgement>)?.Result;
					if (ack == null)
					{
						throw new NotSupportedException($"Expected Handler to return Task<Acknowledgement>. Got {t?.GetType()}");
					}
					var deliveryArgs = context.GetDeliveryEventArgs();
					var channel = context.GetConsumer().Model;

					if (ack is Ack)
					{
						HandleAck(ack as Ack, channel, deliveryArgs);
						return Next.InvokeAsync(context);
					}
					if (ack is Nack)
					{
						HandleNack(ack as Nack, channel, deliveryArgs);
						return Next.InvokeAsync(context);
					}
					if (ack is Reject)
					{
						HandleReject(ack as Reject, channel, deliveryArgs);
						return Next.InvokeAsync(context);
					}
					if (ack is Retry)
					{
						HandleRetry(ack as Retry, channel, deliveryArgs);
						return Next.InvokeAsync(context);
					}

					throw new NotSupportedException($"Unable to handle {ack.GetType()} as an Acknowledgement.");
				})
				.Unwrap();
		}

		protected virtual void HandleAck(Ack ack, IModel channel, BasicDeliverEventArgs deliveryArgs)
		{
			channel.BasicAck(deliveryArgs.DeliveryTag, false);
		}

		protected virtual void HandleNack(Nack nack, IModel channel, BasicDeliverEventArgs deliveryArgs)
		{
			channel.BasicNack(deliveryArgs.DeliveryTag, false, nack.Requeue);
		}

		protected virtual void HandleReject(Reject reject, IModel channel, BasicDeliverEventArgs deliveryArgs)
		{
			channel.BasicReject(deliveryArgs.DeliveryTag, reject.Requeue);
		}

		protected virtual void HandleRetry(Retry retry, IModel channel, BasicDeliverEventArgs deliveryArgs)
		{
			var dlxName = _conventions.RetryLaterExchangeConvention(retry.Span);
			var dlQueueName = _conventions.RetryLaterExchangeConvention(retry.Span);
			channel.ExchangeDeclare(dlxName, ExchangeType.Direct, true, true, null);
			channel.QueueDeclare(dlQueueName, true, false, true, new Dictionary<string, object>
				{
						{QueueArgument.DeadLetterExchange, deliveryArgs.Exchange},
						{QueueArgument.Expires, Convert.ToInt32(retry.Span.Add(TimeSpan.FromSeconds(1)).TotalMilliseconds)},
						{QueueArgument.MessageTtl, Convert.ToInt32(retry.Span.TotalMilliseconds)}
				});
			channel.QueueBind(dlQueueName, dlxName, deliveryArgs.RoutingKey, null);
			channel.BasicPublish(dlxName, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties, deliveryArgs.Body);
			channel.QueueUnbind(dlQueueName, dlxName, deliveryArgs.RoutingKey, null);
		}
	}
}
