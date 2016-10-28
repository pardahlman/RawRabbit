using System.Threading.Tasks;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class MessageInvokationMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
