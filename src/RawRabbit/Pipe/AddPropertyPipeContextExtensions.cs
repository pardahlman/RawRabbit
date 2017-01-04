using System.Threading;

namespace RawRabbit.Pipe
{
	public static class AddPropertyPipeContextExtensions
	{
		public static IPipeContext AddConsumeSemaphore(this IPipeContext context, uint concurrency)
		{
			context.Properties.TryAdd(PipeKey.ConsumeSemaphore, new SemaphoreSlim((int)concurrency));
			return context;
		}


		public static IPipeContext AddConsumeSemaphore(this IPipeContext context, SemaphoreSlim semaphore)
		{
			context.Properties.TryAdd(PipeKey.ConsumeSemaphore, semaphore);
			return context;
		}
	}
}
