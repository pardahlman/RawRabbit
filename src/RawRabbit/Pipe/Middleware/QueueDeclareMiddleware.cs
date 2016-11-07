using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeclareOptions
	{
		public static QueueDeclareOptions For(QueueConfiguration cfg) { return For(context => cfg); }

		public static QueueDeclareOptions For(Func<IPipeContext, QueueConfiguration> func)
		{
			return new QueueDeclareOptions
			{
				QueueFunc = func
			};
		}

		public Func<IPipeContext, QueueConfiguration> QueueFunc { get; set; }
	}

	public class QueueDeclareMiddleware : Middleware
	{
		private readonly Func<IPipeContext, QueueConfiguration> _queueFunc;
		private readonly ITopologyProvider _topology;

		public QueueDeclareMiddleware(ITopologyProvider topology) : this(topology, QueueDeclareOptions.For(c => c.GetQueueConfiguration()))
		{ }

		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options)
		{
			_topology = topology;
			_queueFunc = options.QueueFunc;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var queue = _queueFunc(context);

			if (queue == null)
			{
				throw new ArgumentNullException(nameof(queue));
			}

			return _topology
				.DeclareQueueAsync(queue)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
