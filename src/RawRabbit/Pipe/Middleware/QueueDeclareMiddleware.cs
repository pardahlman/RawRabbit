using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeclareOptions
	{
		public Func<IPipeContext, QueueDeclaration> QueueDeclarationFunc { get; set; }
	}

	public class QueueDeclareMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, QueueDeclaration> QueueDeclareFunc;
		private readonly ITopologyProvider _topology;


		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null )
		{
			_topology = topology;
			QueueDeclareFunc = options?.QueueDeclarationFunc ?? (context => context.GetQueueDeclaration());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var queue = QueueDeclareFunc(context);

			if (queue == null)
			{
				return Next.InvokeAsync(context);
			}

			return _topology
				.DeclareQueueAsync(queue)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
