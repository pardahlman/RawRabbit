using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscriptionOptions
	{
		public Func<IPipeContext, string> QueueNameFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumeFunc{ get; set; }
	}

	public class SubscriptionMiddleware : Pipe.Middleware.Middleware
	{
		private readonly Func<IPipeContext, string> _queueNameFunc;
		private readonly Func<IPipeContext, IBasicConsumer> _consumerFunc;

		public SubscriptionMiddleware(SubscriptionOptions options = null)
		{
			_queueNameFunc = options?.QueueNameFunc ?? (context => context.GetConsumerConfiguration()?.Queue.Name);
			_consumerFunc = options?.ConsumeFunc ?? (context => context.GetConsumer());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = _consumerFunc(context);
			var queueName = _queueNameFunc(context);
			var subscription = new Subscription(consumer, queueName);
			context.Properties.Add(PipeKey.Subscription, subscription);
			return Task.FromResult(subscription);
		}
	}
}
