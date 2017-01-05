using System;
using System.Threading;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;

namespace RawRabbit.Pipe
{
	public static class AddPropertyPipeContextExtensions
	{
		public static IPipeContext UseConsumerConcurrency(this IPipeContext context, uint concurrency)
		{
			context.Properties.TryAdd(PipeKey.ConsumeSemaphore, new SemaphoreSlim((int)concurrency));
			return context;
		}

		public static IPipeContext UseConsumeSemaphore(this IPipeContext context, SemaphoreSlim semaphore)
		{
			context.Properties.TryAdd(PipeKey.ConsumeSemaphore, semaphore);
			return context;
		}

		public static IPipeContext UseConsumerConfiguration(this IPipeContext context, Action<IConsumerConfigurationBuilder> configuration)
		{
			context.Properties.Add(PipeKey.ConfigurationAction, configuration);
			return context;
		}

		public static IPipeContext UsePublisherConfiguration(this IPipeContext context, Action<IPublisherConfigurationBuilder> configuration)
		{
			context.Properties.Add(PipeKey.ConfigurationAction, configuration);
			return context;
		}
	}
}
