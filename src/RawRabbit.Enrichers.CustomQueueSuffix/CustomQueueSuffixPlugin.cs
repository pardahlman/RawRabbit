using System;
using RawRabbit.Configuration.Queue;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.CustomQueueSuffix
{
	public static class CustomQueueSuffixPlugin
	{
		public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, Action<IQueueDeclarationBuilder> queueAction)
		{
			return builder.UseCustomQueueSuffix(new CustomQueueSuffixOptions
			{
				QueueSuffixFunc = context => queueAction
			});
		}

		public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, CustomQueueSuffixOptions options = null)
		{
			if (options == null)
			{
				builder.Register(pipe => pipe.Use<CustomQueueSuffixMiddleware>());
			}
			else
			{
				builder.Register(pipe => pipe.Use<CustomQueueSuffixMiddleware>(options));
			}
			return builder;
		}
	}
}
