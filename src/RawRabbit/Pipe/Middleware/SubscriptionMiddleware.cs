using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Subscription;

namespace RawRabbit.Pipe.Middleware
{
	public class SubscriptionOptions
	{
		public Func<IPipeContext, string> QueueNameFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumeFunc{ get; set; }
		public Action<IPipeContext, ISubscription> SaveInContext { get; set; }
	}

	public class SubscriptionMiddleware : Pipe.Middleware.Middleware
	{
		protected ISubscriptionRepository Repo;
		protected Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, IBasicConsumer> ConsumerFunc;
		protected Action<IPipeContext, ISubscription> SaveInContext;

		public SubscriptionMiddleware(ISubscriptionRepository repo, SubscriptionOptions options = null)
		{
			Repo = repo;
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetConsumerConfiguration()?.Consume.QueueName);
			ConsumerFunc = options?.ConsumeFunc ?? (context => context.GetConsumer());
			SaveInContext = options?.SaveInContext ?? ((ctx, subscription) => ctx.Properties.Add(PipeKey.Subscription, subscription));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = GetConsumer(context);
			var queueName = GetQueueName(context);
			var subscription = CreateSubscription(consumer, queueName);
			SaveSubscriptionInContext(context, subscription);
			SaveSubscriptionInRepo(subscription);
			await Next.InvokeAsync(context, token);
		}

		protected virtual IBasicConsumer GetConsumer(IPipeContext context)
		{
			return ConsumerFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return QueueNameFunc(context);
		}

		protected virtual ISubscription CreateSubscription(IBasicConsumer consumer, string queueName)
		{
			return new Subscription.Subscription(consumer, queueName);
		}

		protected virtual void SaveSubscriptionInContext(IPipeContext context, ISubscription subscription)
		{
			SaveInContext(context, subscription);
		}

		protected virtual void SaveSubscriptionInRepo(ISubscription subscription)
		{
			Repo.Add(subscription);
		}
	}
}
