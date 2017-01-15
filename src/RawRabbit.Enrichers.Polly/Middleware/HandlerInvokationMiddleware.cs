using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class HandlerInvokationMiddleware : Pipe.Middleware.HandlerInvokationMiddleware
	{
		public HandlerInvokationMiddleware(HandlerInvokationOptions options = null)
			: base(options) { }

		protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.HandlerInvokation);
			return policy.ExecuteAsync(
				action: ct => base.InvokeMessageHandler(context, token),
				cancellationToken: token,
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context,
					[RetryKey.CancellationToken] = token
				});
		}
	}
}
