using System;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class HostNameQueueSuffix
	{
		public static IClientBuilder UseHostQueueSuffix(this IClientBuilder builder)
		{
			builder.UseQueueSuffix(new QueueSuffixOptions
			{
				CustomSuffixFunc = context => Environment.MachineName.ToLower(),
				ActiveFunc = context => context.GetHostnameQueueSuffixFlag()
			});

			return builder;
		}
	}
}
