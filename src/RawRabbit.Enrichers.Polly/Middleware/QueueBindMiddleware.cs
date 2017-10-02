using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class QueueBindMiddleware : Pipe.Middleware.QueueBindMiddleware
	{
		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
			: base(topologyProvider, options) { }

		protected override Task BindQueueAsync(string queue, string exchange, string routingKey, IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.QueueBind);
			return policy.ExecuteAsync(
				action: ct => base.BindQueueAsync(queue, exchange, routingKey, context, ct),
				cancellationToken: token,
				contextData: new Dictionary<string, object>
				{
					[RetryKey.TopologyProvider] = TopologyProvider,
					[RetryKey.QueueName] = queue,
					[RetryKey.ExchangeName] = exchange,
					[RetryKey.RoutingKey] = routingKey,
					[RetryKey.PipeContext] = context,
					[RetryKey.CancellationToken] = token
				}
			);
		}
	}
}
