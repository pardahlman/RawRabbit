using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueBindOptions
	{
		public Func<IPipeContext, ConsumerConfiguration> ConsumeFunc { get; set; }

		public static QueueBindOptions For(Func<IPipeContext, ConsumerConfiguration> func)
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
		private readonly Func<IPipeContext, ConsumerConfiguration> _consumeFunc;

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
				.BindQueueAsync(consumerCfg.Queue.QueueName, consumerCfg.Exchange.ExchangeName, consumerCfg.Consume.RoutingKey)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
