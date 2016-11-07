using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class MessageInvokationMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message)
				.ContinueWith(tResponse =>
				{
					context.Properties.Add(RespondKey.ResponseMessage, tResponse.Result);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
