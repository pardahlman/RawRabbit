using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.HostQueueSuffix
{
	public static class HostQueueSuffixPlugin
	{
		public static IClientBuilder UseHostQueueSuffix(this IClientBuilder builder, HostQueueSuffixOptions options = null)
		{
			if (options == null)
			{
				builder.Register(pipe => pipe.Use<HostQueueSuffixMiddleware>());
			}
			else
			{
				builder.Register(pipe => pipe.Use<HostQueueSuffixMiddleware>(options));
			}
			return builder;
		}
	}
}