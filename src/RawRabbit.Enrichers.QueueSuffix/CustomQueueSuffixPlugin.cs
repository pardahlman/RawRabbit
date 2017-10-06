using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.Instantiation;

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
