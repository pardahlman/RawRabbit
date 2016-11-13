using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RawRabbit.Pipe.Middleware
{
	public class AutoAckOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
	}

	public class AutoAckMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected readonly Func<IPipeContext, IBasicConsumer> ConsumerFunc;

		public AutoAckMiddleware(AutoAckOptions options = null)
		{
			DeliveryArgsFunc = options?.DeliveryArgFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			AutoAckMessage(context);
			return Next.InvokeAsync(context);
		}

		protected virtual void AutoAckMessage(IPipeContext context)
		{
			var deliveryArgs = DeliveryArgsFunc(context);
			var channel = ConsumerFunc(context).Model;
			channel.BasicAck(deliveryArgs.DeliveryTag, false);
		}
	}
}
