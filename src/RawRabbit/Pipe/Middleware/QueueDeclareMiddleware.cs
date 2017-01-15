using System;
using System.Collections.Generic;
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
		private readonly ILogger _logger = LogManager.GetLogger<QueueDeclareMiddleware>();

		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null )
		{
			Topology = topology;
			QueueDeclareFunc = options?.QueueDeclarationFunc ?? (context => context.GetQueueDeclaration());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var queue = GetQueueDeclaration(context);

			if (queue == null)
			{
				_logger.LogInformation("Queue will not be declaired: no queue declaration found in context.");
				return Next.InvokeAsync(context, token);
			}

			_logger.LogDebug($"Declaring queue '{queue.Name}'.");
			return DeclareQueueAsync(queue, context, token)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
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
