using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Logging;

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
		protected readonly ITopologyProvider TopologyProvider;
		protected Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;
		private readonly ILog _logger = LogProvider.For<QueueBindMiddleware>();

		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
		{
			TopologyProvider = topologyProvider;
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetConsumeConfiguration()?.QueueName);
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (context => context.GetConsumeConfiguration()?.ExchangeName);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetConsumeConfiguration()?.RoutingKey);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var queueName = GetQueueName(context);
			var exchangeName = GetExchangeName(context);
			var routingKey = GetRoutingKey(context);

			await BindQueueAsync(queueName, exchangeName, routingKey, context, token);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task BindQueueAsync(string queue, string exchange, string routingKey, IPipeContext context, CancellationToken ct)
		{
			return TopologyProvider.BindQueueAsync(queue, exchange, routingKey, context.GetConsumeConfiguration()?.Arguments);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			var routingKey = RoutingKeyFunc(context);
			if (routingKey == null)
			{
				_logger.Warn("Routing key not found in Pipe context.");
			}
			return routingKey;
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			var exchange = ExchangeNameFunc(context);
			if (exchange == null)
			{
				_logger.Warn("Exchange name not found in Pipe context.");
			}
			return exchange;
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			var queue = QueueNameFunc(context);
			if (queue == null)
			{
				_logger.Warn("Queue name not found in Pipe context.");
			}
			return queue;
		}
	}
}
