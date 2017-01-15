using Polly;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public static class PipeContextExtensions
	{
		public static Policy GetPolicy(this IPipeContext context, string policyName = null)
		{
			var fallback = context.Get<Policy>(PolicyKeys.DefaultPolicy);
			return context.Get(policyName, fallback);
		}

		public static IPipeContext UsePolicy(this IPipeContext context, Policy poliocy, string policyName = null)
		{
			policyName = policyName ?? PolicyKeys.DefaultPolicy;
			context.Properties.TryAdd(policyName, poliocy);
			return context;
		}
	}
}
