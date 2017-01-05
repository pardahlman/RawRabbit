using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class ExplicitAckOptions
	{
		public Func<IPipeContext, Task> InvokeMessageHandlerFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<IPipeContext, bool> NoAckFunc { get; set; }
		public Func<IPipeContext, Task> InvokationResultFunc { get; set; }
		public Predicate<Acknowledgement> AbortExecution { get; set; }
	}

	public class ExplicitAckMiddleware : Middleware
	{
		protected INamingConventions Conventions;
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Func<IPipeContext, IBasicConsumer> ConsumerFunc;
		protected Func<IPipeContext, Task> InvokationResultFunc;
		protected Predicate<Acknowledgement> AbortExecution;
		protected Func<IPipeContext, bool> NoAckFunc;

		public ExplicitAckMiddleware(INamingConventions conventions, ExplicitAckOptions options = null)
		{
			Conventions = conventions;
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
			InvokationResultFunc = options?.InvokationResultFunc ?? (context => context.GetMessageHandlerResult());
			AbortExecution = options?.AbortExecution ?? (ack => !(ack is Ack));
			NoAckFunc = options?.NoAckFunc ?? (context => context.GetConsumeConfiguration().NoAck);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var noAck = GetNoAck(context);
			if (noAck)
			{
				return Next.InvokeAsync(context, token);
			}
			var ack = AcknowledgeMessage(context);
			return AbortExecution(ack)
				? Task.FromResult(0)
				: Next.InvokeAsync(context, token);
		}
		
		protected virtual Acknowledgement AcknowledgeMessage(IPipeContext context)
		{
			var ack = (InvokationResultFunc(context) as Task<Acknowledgement>)?.Result;
			if (ack == null)
			{
				throw new NotSupportedException($"Invokation Result of Message Handler not found.");
			}
			var deliveryArgs = DeliveryArgsFunc(context);
			var channel = ConsumerFunc(context).Model;

			if (ack is Ack)
			{
				HandleAck(ack as Ack, channel, deliveryArgs);
				return ack;
			}
			if (ack is Nack)
			{
				HandleNack(ack as Nack, channel, deliveryArgs);
				return ack;
			}
			if (ack is Reject)
			{
				HandleReject(ack as Reject, channel, deliveryArgs);
				return ack;
			}
			if (ack is Retry)
			{
				HandleRetry(ack as Retry, channel, deliveryArgs);
				return ack;
			}

			throw new NotSupportedException($"Unable to handle {ack.GetType()} as an Acknowledgement.");
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
			var dlxName = Conventions.RetryLaterExchangeConvention(retry.Span);
			var dlQueueName = Conventions.RetryLaterExchangeConvention(retry.Span);
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

		protected virtual bool GetNoAck(IPipeContext context)
		{
			return NoAckFunc(context);
		}
	}
}
