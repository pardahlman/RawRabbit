using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
	{
		public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
			: base(options) { }

		protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.HandlerInvocation);
			return policy.ExecuteAsync(
				action: ct => base.InvokeMessageHandler(context, ct),
				cancellationToken: token,
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context,
					[RetryKey.CancellationToken] = token
				});
		}
	}
}
