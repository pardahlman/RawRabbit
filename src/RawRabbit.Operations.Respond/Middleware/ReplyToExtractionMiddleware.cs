using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ReplyToExtractionOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<BasicDeliverEventArgs, PublicationAddress> ReplyToFunc { get; set; }
		public Action<IPipeContext, PublicationAddress> ContextSaveAction { get; set; }
	}

	public class ReplyToExtractionMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Func<BasicDeliverEventArgs, PublicationAddress> ReplyToFunc;
		protected Action<IPipeContext, PublicationAddress> ContextSaveAction;
		private readonly ILog _logger = LogProvider.For<ReplyToExtractionMiddleware>();

		public ReplyToExtractionMiddleware(ReplyToExtractionOptions options = null)
		{
			ContextSaveAction = options?.ContextSaveAction ?? ((ctx, addr) => ctx.Properties.Add(RespondKey.PublicationAddress, addr));
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (ctx => ctx.GetDeliveryEventArgs());
			ReplyToFunc = options?.ReplyToFunc ?? (args =>
				args.BasicProperties.ReplyToAddress ?? new PublicationAddress(ExchangeType.Direct, string.Empty, args.BasicProperties.ReplyTo));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var args = GetDeliveryArgs(context);
			var replyTo = GetReplyTo(args);
			SaveInContext(context, replyTo);
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			var args = DeliveryArgsFunc(context);
			if (args == null)
			{
				_logger.Warn("Delivery args not found in Pipe context.");
			}
			return args;
		}

		protected virtual PublicationAddress GetReplyTo(BasicDeliverEventArgs args)
		{
			var replyTo = ReplyToFunc(args);
			if (replyTo == null)
			{
				_logger.Warn("Reply to address not found in Pipe context.");
			}
			else
			{
				args.BasicProperties.ReplyTo = replyTo.RoutingKey;
				_logger.Info("Using reply address with exchange {exchangeName} and routing key '{routingKey}'", replyTo.ExchangeName, replyTo.RoutingKey);
			}
			return replyTo;
		}

		protected virtual void SaveInContext(IPipeContext context, PublicationAddress replyTo)
		{
			if (ContextSaveAction == null)
			{
				_logger.Warn("No context save action found. Reply to address will not be saved.");
			}
			ContextSaveAction?.Invoke(context, replyTo);
		}
	}
}
