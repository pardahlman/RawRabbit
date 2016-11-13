using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext.Respond.Middleware
{
	public class AutoAckMessageHandlerMiddleware : AutoAckMessageHandlerMiddlewareBase
	{
		public AutoAckMessageHandlerMiddleware(AutoAckHandlerOptions options = null) : base(options)
		{ }

		protected override Task InvokeHandlerAsync(IPipeContext context)
		{
			var handler = context.Get<Func<object, IMessageContext, Task<object>>>(PipeKey.MessageHandler);
			var message = context.GetMessage();
			var msgContext = context.GetMessageContext();

			return handler(message, msgContext);
		}
	}
}
