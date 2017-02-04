using System;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class HostNameQueueSuffix
	{

		public static IClientBuilder UseHostQueueSuffix(this IClientBuilder builder)
		{
			builder.UseCustomQueueSuffix(new QueueSuffixOptions
			{
				CustomSuffixFunc = context => Environment.MachineName.ToLower(),
				ActiveFunc = context => context.GetHostnameQueueSuffixFlag()
			});

			return builder;
		}
	}
}
