using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe
{
	public static class AddPropertyPipeContextExtensions
	{
		public static TPipeContext UseConsumerConcurrency<TPipeContext>(this TPipeContext context, uint concurrency) where TPipeContext : IPipeContext
		{
			var semaphore = new SemaphoreSlim((int)concurrency, (int)concurrency);
			return UseConsumeSemaphore(context, semaphore);
		}

		public static TPipeContext UseConsumeSemaphore<TPipeContext>(this TPipeContext context, SemaphoreSlim semaphore) where TPipeContext : IPipeContext
		{
			return UseThrottledConsume(context, (asyncAction, ct) => semaphore
				.WaitAsync(ct)
				.ContinueWith(tEnter =>
				{
					Task.Run(asyncAction, ct)
						.ContinueWith(tDone => semaphore.Release(), ct);
				}, ct));
		}

		public static TPipeContext UseThrottledConsume<TPipeContext>(this TPipeContext context, Action<Func<Task>, CancellationToken> throttle) where TPipeContext : IPipeContext
		{
			context.Properties.TryAdd(PipeKey.ConsumeThrottleAction, throttle);
			return context;
		}
	}
}
