using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeclareOptions
	{
		public Func<IPipeContext, QueueDeclaration> QueueDeclarationFunc { get; set; }
	}

	public class QueueDeclareMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, QueueDeclaration> QueueDeclareFunc;
		protected readonly ITopologyProvider Topology;
		private readonly ILog _logger = LogProvider.For<QueueDeclareMiddleware>();

		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null )
		{
			Topology = topology;
			QueueDeclareFunc = options?.QueueDeclarationFunc ?? (context => context.GetQueueDeclaration());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default (CancellationToken))
		{
			var queue = GetQueueDeclaration(context);

			if (queue != null)
			{
				_logger.Debug("Declaring queue '{queueName}'.", queue.Name);
				await DeclareQueueAsync(queue, context, token);
			}
			else
			{
				_logger.Info("Queue will not be declaired: no queue declaration found in context.");
			}

			await Next.InvokeAsync(context, token);
		}

		protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
		{
			return QueueDeclareFunc(context);
		}

		protected virtual Task DeclareQueueAsync(QueueDeclaration queue, IPipeContext context, CancellationToken token)
		{
			return Topology.DeclareQueueAsync(queue);
		}
	}
}
