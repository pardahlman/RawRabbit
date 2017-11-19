using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using ExchangeType = RabbitMQ.Client.ExchangeType;

namespace RawRabbit.Middleware
{
	public class RetryLaterOptions
	{
		public Func<IPipeContext, Acknowledgement> AcknowledgementFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	}

	public class RetryLaterMiddleware : StagedMiddleware
	{
		private readonly ILog _logger = LogProvider.For<RetryLaterMiddleware>();
		protected readonly ITopologyProvider TopologyProvider;
		protected readonly INamingConventions Conventions;
		protected readonly IChannelFactory ChannelFactory;
		private readonly IRetryInformationHeaderUpdater _headerUpdater;
		protected Func<IPipeContext, Acknowledgement> AcknowledgementFunc;
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;

		public override string StageMarker => Pipe.StageMarker.HandlerInvoked;

		public RetryLaterMiddleware(ITopologyProvider topology, INamingConventions conventions, IChannelFactory channelFactory, IRetryInformationHeaderUpdater headerUpdater, RetryLaterOptions options = null)
		{
			TopologyProvider = topology;
			Conventions = conventions;
			ChannelFactory = channelFactory;
			_headerUpdater = headerUpdater;
			AcknowledgementFunc = options?.AcknowledgementFunc ?? (context => context.GetMessageAcknowledgement());
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var ack = GetMessageAcknowledgement(context);
			if (!(ack is Retry retryAck))
			{
				await Next.InvokeAsync(context, token);
				return;
			}

			var deadLeterExchangeName = GetDeadLetterExchangeName(retryAck.Span);
			await TopologyProvider.DeclareExchangeAsync(new ExchangeDeclaration
			{
				Name = deadLeterExchangeName,
				Durable = true,
				ExchangeType = ExchangeType.Direct
			});

			var deliveryArgs = GetDeliveryEventArgs(context);
			_logger.Info("Message is marked for Retry. Will be published on exchange {exchangeName} with routing key {routingKey} in {retryIn}", deliveryArgs.Exchange, deliveryArgs.RoutingKey, retryAck.Span);
			UpdateRetryHeaders(deliveryArgs, context);
			var deadLetterQueueName = GetDeadLetterQueueName(deliveryArgs.Exchange, retryAck.Span);
			await TopologyProvider.DeclareQueueAsync(new QueueDeclaration
			{
				Name = deadLetterQueueName,
				Durable = true,
				Arguments = new Dictionary<string, object>
				{
					{QueueArgument.DeadLetterExchange, deliveryArgs.Exchange},
					{QueueArgument.Expires, Convert.ToInt32(retryAck.Span.Add(TimeSpan.FromSeconds(1)).TotalMilliseconds)},
					{QueueArgument.MessageTtl, Convert.ToInt32(retryAck.Span.TotalMilliseconds)}
				}
			});
			await TopologyProvider.BindQueueAsync(deadLetterQueueName, deadLeterExchangeName, deliveryArgs.RoutingKey);
			using (var publishChannel = await ChannelFactory.CreateChannelAsync(token))
			{
				publishChannel.BasicPublish(deadLeterExchangeName, deliveryArgs.RoutingKey, false, deliveryArgs.BasicProperties, deliveryArgs.Body);
			}
			await TopologyProvider.UnbindQueueAsync(deadLetterQueueName, deadLeterExchangeName, deliveryArgs.RoutingKey);

			context.Properties.AddOrReplace(PipeKey.MessageAcknowledgement, new Ack());
			await Next.InvokeAsync(context, token);
		}

		private string GetDeadLetterQueueName(string originalExchangeName, TimeSpan retryAckSpan)
		{
			return Conventions.RetryLaterQueueNameConvetion(originalExchangeName, retryAckSpan);
		}

		protected virtual Acknowledgement GetMessageAcknowledgement(IPipeContext context)
		{
			return AcknowledgementFunc?.Invoke(context);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryEventArgs(IPipeContext context)
		{
			return DeliveryArgsFunc?.Invoke(context);
		}

		protected virtual string GetDeadLetterExchangeName(TimeSpan retryIn)
		{
			return Conventions.RetryLaterExchangeConvention(retryIn);
		}

		protected virtual TimeSpan GetRetryTimeSpan(IPipeContext context)
		{
			return (GetMessageAcknowledgement(context) as Retry)?.Span ?? new TimeSpan(-1);
		}

		protected virtual void UpdateRetryHeaders(BasicDeliverEventArgs args, IPipeContext context)
		{
			var retryInfo = context.GetRetryInformation();
			_headerUpdater.AddOrUpdate(args, retryInfo);
		}
	}
}
