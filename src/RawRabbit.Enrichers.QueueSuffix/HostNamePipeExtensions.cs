using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class HostNamePipeExtensions
	{
		private const string HostnameQueueSuffixActive = "HostnameQueueSuffixActive";

		public static bool GetHostnameQueueSuffixFlag(this IPipeContext context)
		{
			return context.Get(HostnameQueueSuffixActive, true);
		}

		public static TPipeContext UseHostnameQueueSuffix<TPipeContext>(this TPipeContext context, bool activated) where TPipeContext : IPipeContext
		{
			context.Properties.TryAdd(HostnameQueueSuffixActive, activated);
			return context;
		}
	}
}
