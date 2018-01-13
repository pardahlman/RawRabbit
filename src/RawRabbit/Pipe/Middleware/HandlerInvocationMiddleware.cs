using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class HandlerInvocationOptions
	{
		public Func<IPipeContext, Func<object[], Task<Acknowledgement>>> MessageHandlerFunc { get; set; }
		public Func<IPipeContext, object[]> HandlerArgsFunc { get; set; }
		public Action<IPipeContext, Acknowledgement> PostInvokeAction { get; set; }
	}

	public class HandlerInvocationMiddleware : Middleware
	{
		protected Func<IPipeContext, object[]> HandlerArgsFunc;
		protected Action<IPipeContext, Acknowledgement> PostInvokeAction;
		protected Func<IPipeContext, Func<object[], Task<Acknowledgement>>> MessageHandlerFunc;

		public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
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

		protected virtual async Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var args = HandlerArgsFunc(context);
			var handler = MessageHandlerFunc(context);

			var acknowledgement = await handler(args);
			context.Properties.TryAdd(PipeKey.MessageAcknowledgement, acknowledgement);
			PostInvokeAction?.Invoke(context, acknowledgement);
		}
	}
}
