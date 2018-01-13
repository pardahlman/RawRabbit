using RawRabbit.Common;
using RawRabbit.Instantiation;
using RawRabbit.Middleware;

namespace RawRabbit
{
	public static class RetryLaterPlugin
	{
		public static IClientBuilder UseRetryLater(this IClientBuilder builder)
		{
			builder.Register(
				pipe => pipe
					.Use<RetryLaterMiddleware>()
					.Use<RetryInformationExtractionMiddleware>(),
				ioc => ioc
					.AddSingleton<IRetryInformationHeaderUpdater, RetryInformationHeaderUpdater>()
					.AddSingleton<IRetryInformationProvider, RetryInformationProvider>()
				);
			return builder;
		}
	}
}
