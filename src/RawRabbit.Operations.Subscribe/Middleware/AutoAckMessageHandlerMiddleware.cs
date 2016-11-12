using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class AutoAckHandlerOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
	}

	public class AutoAckMessageHandlerMiddleware : Pipe.Middleware.Middleware
	{
		private readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgFunc;
		private readonly Func<IPipeContext, IModel> _channelFunc;

		public AutoAckMessageHandlerMiddleware(AutoAckHandlerOptions options = null)
		{
			_deliveryArgFunc = options?.DeliveryArgFunc ?? (context => context.GetDeliveryEventArgs());
			_channelFunc = options?.ChannelFunc ?? (context => context.GetConsumer().Model);
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			return InvokeMessageHandlerAsync(context)
				.ContinueWith(t =>
				{
					var deliveryArgs = _deliveryArgFunc(context);
					var channel = _channelFunc(context);
					channel.BasicAck(deliveryArgs.DeliveryTag, false);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}

		protected virtual Task InvokeMessageHandlerAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler.Invoke(message);
		}
	}
}
