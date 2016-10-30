using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscriptionMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var consumer = context.GetConsumer();
			var queue = context.GetQueueConfiguration();
			var subscription = new Subscription(consumer, queue.FullQueueName);
			context.Properties.Add(PipeKey.Subscription, subscription);
			return Task.FromResult(subscription);
		}
	}
}
