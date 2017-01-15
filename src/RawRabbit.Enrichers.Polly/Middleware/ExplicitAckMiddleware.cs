using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
	{
		public ExplicitAckMiddleware(INamingConventions conventions, ExplicitAckOptions options = null)
				: base(conventions, options) { }

		protected override Acknowledgement AcknowledgeMessage(IPipeContext context)
		{
			var policy = context.GetPolicy(PolicyKeys.MessageAcknowledge);
			return policy.Execute(
				action: () => base.AcknowledgeMessage(context),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context
				});
		}
	}
}
