using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
	public class MessageHandlerInvokationMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var messageContext = context.GetMessageContext();

			var handler = context.Get<Func<object, IMessageContext,Task>>(PipeKey.MessageHandler);

			return handler
				.Invoke(message, messageContext)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
