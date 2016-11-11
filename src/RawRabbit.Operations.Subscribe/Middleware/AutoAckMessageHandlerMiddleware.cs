using System.Threading.Tasks;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class AutoAckMessageHandlerMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message)
				.ContinueWith(t =>
				{
					var deliveryArgs = context.GetDeliveryEventArgs();
					var channel = context.GetConsumer().Model;
					channel.BasicAck(deliveryArgs.DeliveryTag, false);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
