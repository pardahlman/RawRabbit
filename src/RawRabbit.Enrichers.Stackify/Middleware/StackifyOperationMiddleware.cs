using System;

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using StackifyLib;

namespace RawRabbit.Enrichers.Stackify.Middleware
{
	public class StackifyOperationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
	{
		public StackifyOperationMiddleware(HandlerInvocationOptions options = null)
			: base(options) { }

		protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			Debug.WriteLine(context.GetMessage().GetType().ToString());
			Debug.WriteLine(context.Get<string>("GlobalExecutionId") ?? Guid.NewGuid().ToString());
			var tracer = ProfileTracer.CreateAsOperation(
				context.GetMessage().GetType().ToString(),
				context.Get<string>("GlobalExecutionId") ?? Guid.NewGuid().ToString()); // Added support for GlobalExecutionIdEnricher
			return tracer.ExecAsync(async () => await base.InvokeMessageHandler(context, token));
		}
	}
}
