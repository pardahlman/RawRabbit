using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class CustomQueueSuffixPlugin
	{
		public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, string suffix = null)
		{
			var options =  new QueueSuffixOptions
			{
				CustomSuffixFunc = context => suffix,
				ActiveFunc = context => context.GetCustomQueueSuffixActivated(),
				ContextSuffixOverrideFunc = context => context.GetCustomQueueSuffix()
			};
			builder.UseQueueSuffix(options);

			return builder;
		}
	}
}
