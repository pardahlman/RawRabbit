using System;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class QueueSuffixPlugin
	{
		public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, string suffix)
		{
			return builder.UseCustomQueueSuffix(new QueueSuffixOptions
			{
				CustomSuffixFunc = context => suffix
			});
		}

		public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, QueueSuffixOptions options = null)
		{
			if (options == null)
			{
				builder.Register(pipe => pipe.Use<QueueSuffixMiddleware>());
			}
			else
			{
				builder.Register(pipe => pipe.Use<QueueSuffixMiddleware>(options));
			}
			return builder;
		}
	}
}
