using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class SubscribeContextExtensions
	{
		public static IPipeContext UseSubscribeConfiguration(this IPipeContext context, Action<IConsumerConfigurationBuilder> configuration)
		{
			context.Properties.Add(PipeKey.ConfigurationAction, configuration);
			return context;
		}
	}
}
