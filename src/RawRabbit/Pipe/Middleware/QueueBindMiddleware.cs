using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueBindOptions
	{
		public Func<IPipeContext, ConsumeConfiguration> ConsumeFunc { get; set; }

		public static QueueBindOptions For(Func<IPipeContext, ConsumeConfiguration> func)
		{
			return new QueueBindOptions
			{
				ConsumeFunc = func
			};
		}
	}

	public class QueueBindMiddleware : Middleware
	{
		private readonly ITopologyProvider _topologyProvider;
		private readonly Func<IPipeContext, ConsumeConfiguration> _consumeFunc;

		public QueueBindMiddleware(ITopologyProvider topology) : this(topology, QueueBindOptions.For(c => c.GetConsumerConfiguration()))
		{ }

		public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options)
		{
			_topologyProvider = topologyProvider;
			_consumeFunc = options.ConsumeFunc;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumerCfg = _consumeFunc(context);

			return _topologyProvider
				.BindQueueAsync(consumerCfg.Queue, consumerCfg.Exchange, consumerCfg.RoutingKey)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
