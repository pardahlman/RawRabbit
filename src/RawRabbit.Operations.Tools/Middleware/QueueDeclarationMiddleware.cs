using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Tools.Middleware
{
	public class QueueDeclarationOptions
	{
		public Func <IPipeContext, QueueDeclaration> QueueDeclarationFunc { get; set; }
		public Action<IPipeContext, QueueDeclaration> SaveToContext { get; set;  }
	}

	public class QueueDeclarationMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly IQueueConfigurationFactory CfgFactory;
		protected Func<IPipeContext, QueueDeclaration> QueueDeclarationFunc;
		protected Action<IPipeContext, QueueDeclaration> SaveToContextAction;

		public QueueDeclarationMiddleware(IQueueConfigurationFactory cfgFactory, QueueDeclarationOptions options = null)
		{
			CfgFactory = cfgFactory;
			QueueDeclarationFunc = options?.QueueDeclarationFunc ?? (ctx =>ctx.GetQueueDeclaration());
			SaveToContextAction = options?.SaveToContext ?? ((ctx, declaration) =>ctx.Properties.TryAdd(PipeKey.QueueDeclaration, declaration));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var queueDeclaration = GetQueueDeclaration(context);
			SaveToContext(context, queueDeclaration);
			return Next.InvokeAsync(context, token);
		}

		protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
		{
			var declaration = QueueDeclarationFunc?.Invoke(context);
			if(declaration != null)
			{
				return declaration;
			}
			var messageType = context.GetMessageType();
			return CfgFactory.Create(messageType);
		}

		protected virtual void SaveToContext(IPipeContext context, QueueDeclaration declaration)
		{
			SaveToContextAction?.Invoke(context, declaration);
		}
	}
}
