using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
	{
		public ExplicitAckMiddleware(INamingConventions conventions, ExplicitAckOptions options = null)
				: base(conventions, options) { }

		protected override async Task<Acknowledgement> AcknowledgeMessage(IPipeContext context)
		{
			var policy = context.GetPolicy(PolicyKeys.MessageAcknowledge);
			var result = await policy.ExecuteAsync(
				action: () => { return Task.FromResult(base.AcknowledgeMessage(context)); },
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context
				});
			return await result;
		}
	}
}
