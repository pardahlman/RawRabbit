using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class AutoAckOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, bool> NoAckFunc { get; set; }
	}

	public class AutoAckMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected readonly Func<IPipeContext, IBasicConsumer> ConsumerFunc;
		protected Func<IPipeContext, bool> NoAckFunc;
		private readonly ILog _logger = LogProvider.For<AutoAckMiddleware>();

		public AutoAckMiddleware(AutoAckOptions options = null)
		{
			DeliveryArgsFunc = options?.DeliveryArgFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
			NoAckFunc = options?.NoAckFunc ?? (context => context.GetConsumeConfiguration()?.NoAck ?? false);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var noAck = GetNoAck(context);
			if (noAck)
			{
				_logger.Debug("NoAck is enabled, continuing without sending ack.");
			}
			else
			{
				var deliveryArgs = GetDeliveryArgs(context);
				var channel = GetChannel(context);
				AckMessage(channel, deliveryArgs);
			}

			await Next.InvokeAsync(context, token);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			var args = DeliveryArgsFunc(context);
			if (args == null)
			{
				_logger.Warn("Unable to extract delivery args from Pipe context.");
			}
			return args;
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			var consumer = ConsumerFunc(context);
			if (consumer == null)
			{
				_logger.Warn("Unable to find consumer in Pipe context.");
			}
			return consumer?.Model;
		}

		protected virtual void AckMessage(IModel channel, BasicDeliverEventArgs args)
		{
			channel.BasicAck(args.DeliveryTag, false);
		}

		protected virtual bool GetNoAck(IPipeContext context)
		{
			return NoAckFunc(context);
		}
	}
}
