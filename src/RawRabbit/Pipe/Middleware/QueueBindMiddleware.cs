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
		private readonly ILogger _logger = LogManager.GetLogger<QueueBindMiddleware>();

		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
		{
			TopologyProvider = topologyProvider;
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetQueueDeclaration()?.Name);
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (context => context.GetExchangeDeclaration()?.Name);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetConsumeConfiguration()?.RoutingKey);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var queueName = GetQueueName(context);
			var exchangeName = GetExchangeName(context);
			var routingKey = GetRoutingKey(context);

			return BindQueueAsync(queueName, exchangeName, routingKey)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}

		protected virtual Task BindQueueAsync(string queueName, string exchangeName, string routingKey)
		{
			return TopologyProvider.BindQueueAsync(queueName, exchangeName, routingKey);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			var routingKey = RoutingKeyFunc(context);
			if (routingKey == null)
			{
				_logger.LogWarning("Routing key not found in Pipe context.");
			}
			return routingKey;
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			var exchange = ExchangeNameFunc(context);
			if (exchange == null)
			{
				_logger.LogWarning("Exchange name not found in Pipe context.");
			}
			return exchange;
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			var queue = QueueNameFunc(context);
			if (queue == null)
			{
				_logger.LogWarning("Queue name not found in Pipe context.");
			}
			return queue;
		}
	}
}
