using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
    public class ExplicitAckMessageHandlerMiddleware : Operations.Subscribe.Middleware.ExplicitAckMessageHandlerMiddleware
	{
		public ExplicitAckMessageHandlerMiddleware(INamingConventions conventions)
			: base(conventions) { }

		protected override Task InvokeMessageHandlerAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var messageContext = context.GetMessageContext();

			var handler = context.Get<Func<object, IMessageContext, Task>>(PipeKey.MessageHandler);

			return handler.Invoke(message, messageContext);
		}
	}
}
