using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class AutoAckMessageHandlerMiddleware : AutoAckMessageHandlerMiddlewareBase
	{
		public AutoAckMessageHandlerMiddleware(AutoAckHandlerOptions options = null) : base(options)
		{ }

		protected override Task InvokeHandlerAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var handler = context.GetMessageHandler();

			return handler
				.Invoke(message)
				.ContinueWith(tResponse => context.Properties.Add(RespondKey.ResponseMessage, tResponse.Result));
		}
	}
}
