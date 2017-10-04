using System;
using System.Threading;
using System.Threading.Tasks;

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
	}
}
