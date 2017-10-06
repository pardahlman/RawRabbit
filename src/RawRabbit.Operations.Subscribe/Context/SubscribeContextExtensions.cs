using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class SubscribeContextExtensions
	{
		public static ISubscribeContext UseSubscribeConfiguration(this ISubscribeContext context, Action<IConsumerConfigurationBuilder> configuration)
		{
			context.Properties.Add(PipeKey.ConfigurationAction, configuration);
			return context;
		}
	}
}
