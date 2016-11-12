using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
	public class AutoAckMessageHandlerMiddleware : Operations.Subscribe.Middleware.AutoAckMessageHandlerMiddleware
	{
		public AutoAckMessageHandlerMiddleware(AutoAckHandlerOptions options = null) : base(options)
		{ }

		protected override Task InvokeMessageHandlerAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var messageContext = context.GetMessageContext();

			var handler = context.Get<Func<object, IMessageContext,Task>>(PipeKey.MessageHandler);

			return handler.Invoke(message, messageContext);
		}
	}
}
