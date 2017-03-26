using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class HandlerInvokationOptions
	{
		public Func<IPipeContext, Func<object[], Task>> MessageHandlerFunc { get; set; }
		public Func<IPipeContext, object[]> HandlerArgsFunc { get; set; }
		public Action<IPipeContext, Task> PostInvokeAction { get; set; }
	}

	public class HandlerInvokationMiddleware : Middleware
	{
		protected Func<IPipeContext, object[]> HandlerArgsFunc;
		protected Action<IPipeContext, Task> PostInvokeAction;
		protected Func<IPipeContext, Func<object[], Task>> MessageHandlerFunc;

		public HandlerInvokationMiddleware(HandlerInvokationOptions options = null)
		{
			HandlerArgsFunc = options?.HandlerArgsFunc ?? (context => context.GetMessageHandlerArgs()) ;
			MessageHandlerFunc = options?.MessageHandlerFunc ?? (context => context.GetMessageHandler());
			PostInvokeAction = options?.PostInvokeAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			await InvokeMessageHandler(context, token);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var args = HandlerArgsFunc(context);
			var handler = MessageHandlerFunc(context);

			return handler(args)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
					{
						throw t.Exception;
					}
					context.Properties.TryAdd(PipeKey.MessageHandlerResult, t);
					PostInvokeAction?.Invoke(context, t);
					return t;
				}, token)
				.Unwrap();
		}
	}
}
