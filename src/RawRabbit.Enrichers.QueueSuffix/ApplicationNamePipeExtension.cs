using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class ApplicationNamePipeExtension
	{
		private const string ApplicationQueueSuffix = "ApplicationQueueSuffix";

		public static TPipeContext UseApplicationQueueSuffix<TPipeContext>(this TPipeContext context, bool use = true) where TPipeContext : IPipeContext
		{
			context.Properties.TryAdd(ApplicationQueueSuffix, use);
			return context;
		}

		public static bool GetApplicationSuffixFlag(this IPipeContext context)
		{
			return context.Get(ApplicationQueueSuffix, true);
		}
	}
}
