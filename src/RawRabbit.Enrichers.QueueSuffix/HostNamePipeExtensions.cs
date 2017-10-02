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

		public static IPipeContext UseHostnameQueueSuffix(this IPipeContext context, bool activated)
		{
			context.Properties.TryAdd(HostnameQueueSuffixActive, activated);
			return context;
		}
	}
}
