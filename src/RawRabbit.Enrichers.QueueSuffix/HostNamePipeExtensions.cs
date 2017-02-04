using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class HostNamePipeExtensions
	{
		private const string HostnameQueueSuffix = "HostnameQueueSuffix";

		public static bool GetHostnameQueueSuffixFlag(this IPipeContext context)
		{
			return context.Get(HostnameQueueSuffix, true);
		}

		public static IPipeContext UseHostnameQueueSuffix(this IPipeContext context, bool activated)
		{
			context.Properties.TryAdd(HostnameQueueSuffix, activated);
			return context;
		}


	}
}
