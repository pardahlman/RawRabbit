using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.GlobalExecutionId.Dependencies;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class PersistGlobalExecutionIdOptions
	{
		public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
	}

	public class PersistGlobalExecutionIdMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, string> ExecutionIdFunc;

		public override string StageMarker => Pipe.StageMarker.MessageReceived;

		public PersistGlobalExecutionIdMiddleware(PersistGlobalExecutionIdOptions options = null)
		{
			ExecutionIdFunc = options?.ExecutionIdFunc ?? (context => context.GetGlobalExecutionId());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var globalExecutionId = GetGlobalExecutionId(context);
			PersistInProcess(globalExecutionId);
			return Next.InvokeAsync(context, token);
		}

		protected virtual string GetGlobalExecutionId(IPipeContext context)
		{
			return ExecutionIdFunc(context);
		}

		protected virtual void PersistInProcess(string id)
		{
			GlobalExecutionIdRepository.Set(id);
		}
	}
}
