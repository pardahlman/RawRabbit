using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.ApplicationQueueSuffix
{
	public static class ApplicationQueueSuffixPlugin
	{
		public static IClientBuilder UseApplicationQueueSuffix(this IClientBuilder builder, ApplicationQueueSuffixOptions options = null)
		{
			if (options == null)
			{
				builder.Register(pipe => pipe.Use<ApplicationQueueSuffixMiddleware>());
			}
			else
			{
				builder.Register(pipe => pipe.Use<ApplicationQueueSuffixMiddleware>(options));
			}
			return builder;
		}
	}
}
