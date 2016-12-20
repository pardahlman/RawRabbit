using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class SubscriptionOptions
	{
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, QueueDeclaration> QueueFunc { get; set; }
	}

	public class SubscriptionMiddleware : Pipe.Middleware.Middleware
	{
		private readonly Func<IPipeContext, IBasicConsumer> _consumerFunc;
		private readonly Func<IPipeContext, QueueDeclaration> _queueFunc;

		public SubscriptionMiddleware(SubscriptionOptions options = null)
		{
			_consumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
			_queueFunc = options?.QueueFunc ?? (context => context.GetQueueDeclaration());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = _consumerFunc(context);
			var queue = _queueFunc(context);
			var subscription = new Subscription(consumer, queue.FullQueueName);
			context.Properties.Add(PipeKey.Subscription, subscription);
			return Task.FromResult(subscription);
		}
	}
}
