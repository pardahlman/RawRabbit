using System;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class PublishContextExtensions
	{
		public static IPipeContext UsePublishConfiguration(this IPipeContext context, Action<IPublisherConfigurationBuilder> configuration)
		{
			context.Properties.Add(PipeKey.ConfigurationAction, configuration);
			return context;
		}
	}
}
