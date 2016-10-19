using System;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageInvokationMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message, msgContext)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
