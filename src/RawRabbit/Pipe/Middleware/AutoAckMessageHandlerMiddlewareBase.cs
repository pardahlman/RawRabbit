using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RawRabbit.Pipe.Middleware
{
	public class AutoAckHandlerOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
	}

	public abstract class AutoAckMessageHandlerMiddlewareBase : Middleware
	{
		protected readonly Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgFunc;
		protected readonly Func<IPipeContext, IModel> ChannelFunc;

		protected AutoAckMessageHandlerMiddlewareBase(AutoAckHandlerOptions options = null)
		{
			DeliveryArgFunc = options?.DeliveryArgFunc ?? (context => context.GetDeliveryEventArgs());
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetConsumer().Model);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return InvokeHandlerAsync(context)
				.ContinueWith(tResponse =>
				{
					AutoAckMessage(context);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}

		protected virtual void AutoAckMessage(IPipeContext context)
		{
			var deliveryArgs = DeliveryArgFunc(context);
			var channel = ChannelFunc(context);
			channel.BasicAck(deliveryArgs.DeliveryTag, false);
		}

		protected abstract Task InvokeHandlerAsync(IPipeContext context);
	}
}
