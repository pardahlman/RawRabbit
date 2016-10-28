using System;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeclareMiddleware : Middleware
	{
		private readonly ITopologyProvider _topology;

		public QueueDeclareMiddleware(ITopologyProvider topology)
		{
			_topology = topology;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var queue = context.GetQueueConfiguration();

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
