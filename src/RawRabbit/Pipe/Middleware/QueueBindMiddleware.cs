using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueBindOptions
	{
		public Func<IPipeContext, string> QueueNameFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
	}

	public class QueueBindMiddleware : Middleware
	{
		private readonly ITopologyProvider _topologyProvider;
		protected Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;

		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
		{
			_topologyProvider = topologyProvider;
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetQueueDeclaration()?.Name);
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (context => context.GetExchangeDeclaration()?.Name);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetConsumeConfiguration()?.RoutingKey);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var queueName = GetQueueName(context);
			var exchangeName = GetExchangeName(context);
			var routingKey = GetRoutingKey(context);

			return BindQueueAsync(queueName, exchangeName, routingKey)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}

		protected virtual Task BindQueueAsync(string queueName, string exchangeName, string routingKey)
		{
			return _topologyProvider.BindQueueAsync(queueName, exchangeName, routingKey);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			return RoutingKeyFunc(context);
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			return ExchangeNameFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return QueueNameFunc(context);
		}
	}
}
