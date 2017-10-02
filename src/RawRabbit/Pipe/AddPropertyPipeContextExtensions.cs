using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;

namespace RawRabbit.Pipe
{
	public static class AddPropertyPipeContextExtensions
	{
		public static IPipeContext UseConsumerConcurrency(this IPipeContext context, uint concurrency)
		{
			var semaphore = new SemaphoreSlim((int)concurrency, (int)concurrency);
			return UseConsumeSemaphore(context, semaphore);
		}

		public static IPipeContext UseConsumeSemaphore(this IPipeContext context, SemaphoreSlim semaphore)
		{
			return UseThrottledConsume(context, (asyncAction, ct) => semaphore
				.WaitAsync(ct)
				.ContinueWith(tEnter =>
				{
					Task.Run(asyncAction, ct)
						.ContinueWith(tDone => semaphore.Release(), ct);
				}, ct));
		}

		public static IPipeContext UseThrottledConsume(this IPipeContext context, Action<Func<Task>, CancellationToken> throttle)
		{
			context.Properties.TryAdd(PipeKey.ConsumeThrottleAction, throttle);
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
