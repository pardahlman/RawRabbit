using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueBindMiddleware : Middleware
	{
		private readonly ITopologyProvider _topologyProvider;

		public QueueBindMiddleware(ITopologyProvider topologyProvider)
		{
			_topologyProvider = topologyProvider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumerCfg = context.GetConsumerConfiguration();

			return _topologyProvider
				.BindQueueAsync(consumerCfg.Queue, consumerCfg.Exchange, consumerCfg.RoutingKey)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
