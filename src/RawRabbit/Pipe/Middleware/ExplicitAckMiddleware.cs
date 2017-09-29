using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using ExchangeType = RabbitMQ.Client.ExchangeType;

namespace RawRabbit.Pipe.Middleware
{
	public class ExplicitAckOptions
	{
		public Func<IPipeContext, Task> InvokeMessageHandlerFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<IPipeContext, bool> AutoAckFunc { get; set; }
		public Func<IPipeContext, Task> InvocationResultFunc { get; set; }
		public Predicate<Acknowledgement> AbortExecution { get; set; }
	}

	public class ExplicitAckMiddleware : Middleware
	{
		protected INamingConventions Conventions;
		protected readonly ITopologyProvider Topology;
		protected readonly IChannelFactory ChannelFactory;
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Func<IPipeContext, IBasicConsumer> ConsumerFunc;
		protected Func<IPipeContext, Task> InvocationResultFunc;
		protected Predicate<Acknowledgement> AbortExecution;
		protected Func<IPipeContext, bool> AutoAckFunc;

		public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
		{
			Conventions = conventions;
			Topology = topology;
			ChannelFactory = channelFactory;
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
			InvocationResultFunc = options?.InvocationResultFunc ?? (context => context.GetMessageHandlerResult());
			AbortExecution = options?.AbortExecution ?? (ack => !(ack is Ack));
			AutoAckFunc = options?.AutoAckFunc ?? (context => context.GetConsumeConfiguration().AutoAck);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var autoAck = GetAutoAck(context);
			if (!autoAck)
			{
				var ack = await AcknowledgeMessageAsync(context);
				if (AbortExecution(ack))
				{
					return;
				}
			}
			await Next.InvokeAsync(context, token);
		}

		protected virtual async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
		{
			var ack = (InvocationResultFunc(context) as Task<Acknowledgement>)?.Result;
			if (ack == null)
			{
				throw new NotSupportedException($"Invocation Result of Message Handler not found.");
			}
			var deliveryArgs = DeliveryArgsFunc(context);
			var channel = ConsumerFunc(context).Model;

			if (channel == null)
			{
				throw new NullReferenceException("Unable to retrieve channel for delivered message.");
			}

			if (!channel.IsOpen)
			{
				if (channel is IRecoverable recoverable)
				{
					var recoverTsc = new TaskCompletionSource<bool>();

					EventHandler<EventArgs> OnRecover = null;
					OnRecover = (sender, args) =>
					{
						recoverTsc.TrySetResult(true);
						recoverable.Recovery -= OnRecover;
					};
					recoverable.Recovery += OnRecover;
					await recoverTsc.Task;
				}
			}

			if (channel.NextPublishSeqNo < deliveryArgs.DeliveryTag)
			{
				return new Ack();
			}

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
				await HandleRetryAsync(ack as Retry, channel, deliveryArgs);
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

		protected virtual async Task HandleRetryAsync(Retry retry, IModel channel, BasicDeliverEventArgs deliveryArgs)
		{
			channel.BasicAck(deliveryArgs.DeliveryTag, false);

			var deadLeterExchangeName = Conventions.RetryLaterExchangeConvention(retry.Span);
			var deadLetterQueueName = Conventions.RetryLaterQueueNameConvetion(deliveryArgs.Exchange, retry.Span);
			await Topology.DeclareExchangeAsync(new ExchangeDeclaration
			{
				Name = deadLeterExchangeName,
				Durable = true,
				ExchangeType = ExchangeType.Direct
			});
			await Topology.DeclareQueueAsync(new QueueDeclaration
			{
				Name = deadLetterQueueName,
				Durable = true,
				Arguments = new Dictionary<string, object>
				{
					{QueueArgument.DeadLetterExchange, deliveryArgs.Exchange},
					{QueueArgument.Expires, Convert.ToInt32(retry.Span.Add(TimeSpan.FromSeconds(1)).TotalMilliseconds)},
					{QueueArgument.MessageTtl, Convert.ToInt32(retry.Span.TotalMilliseconds)}
				}
			});
			await Topology.BindQueueAsync(deadLetterQueueName, deadLeterExchangeName, deliveryArgs.RoutingKey);
			using (var publishChannel = await ChannelFactory.CreateChannelAsync())
			{
				publishChannel.BasicPublish(deadLeterExchangeName, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties, deliveryArgs.Body);
			}
			await Topology.UnbindQueueAsync(deadLetterQueueName, deadLeterExchangeName, deliveryArgs.RoutingKey);
		}

		protected virtual bool GetAutoAck(IPipeContext context)
		{
			return AutoAckFunc(context);
		}
	}
}
