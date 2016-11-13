using System;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageHandlerInvokationOptions
	{
		public Func<IPipeContext, Func<object[], Task>> MessageHandlerFunc { get; set; }
		public Func<IPipeContext, object[]> HandlerArgsFunc { get; set; }
		public Action<IPipeContext, Task> PostInvokeAction { get; set; }
	}

	public class MessageHandlerInvokationMiddleware : Middleware
	{
		protected Func<IPipeContext, object[]> HandlerArgsFunc;
		protected Action<IPipeContext, Task> PostInvokeAction;
		protected Func<IPipeContext, Func<object[], Task>> MessageHandlerFunc;

		public MessageHandlerInvokationMiddleware(MessageHandlerInvokationOptions options = null)
		{
			HandlerArgsFunc = options?.HandlerArgsFunc ?? (context => context.GetMessageHandlerArgs()) ;
			MessageHandlerFunc = options?.MessageHandlerFunc ?? (context => context.GetMessageHandler());
			PostInvokeAction = options?.PostInvokeAction;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return InvokeMessageHandler(context)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}

		protected virtual Task InvokeMessageHandler(IPipeContext context)
		{
			var args = HandlerArgsFunc(context);
			var handler = MessageHandlerFunc(context);

			return handler(args)
				.ContinueWith(t =>
				{
					context.Properties.TryAdd(PipeKey.MessageHandlerResult, t);
					PostInvokeAction?.Invoke(context, t);
					return t;
				})
				.Unwrap();
		}
	}
}
