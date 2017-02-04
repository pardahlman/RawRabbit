using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.GlobalExecutionId.Dependencies;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class AppendGlobalExecutionIdOptions
	{
		public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
		public Action<IPipeContext, string> SaveInContext { get; set; }
	}

	public class AppendGlobalExecutionIdMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.ProducerInitialized;
		protected Func<IPipeContext, string> ExecutionIdFunc;
		protected Action<IPipeContext, string> SaveInContext;
		private readonly ILogger _logger = LogManager.GetLogger<AppendGlobalExecutionIdMiddleware>();

		public AppendGlobalExecutionIdMiddleware(AppendGlobalExecutionIdOptions options = null)
		{
			ExecutionIdFunc = options?.ExecutionIdFunc ?? (context => context.GetGlobalExecutionId());
			SaveInContext = options?.SaveInContext ?? ((ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var fromContext = GetExecutionIdFromContext(context);
			if (!string.IsNullOrWhiteSpace(fromContext))
			{
				_logger.LogInformation($"GlobalExecutionId '{fromContext}' was allready found in PipeContext.");
				return Next.InvokeAsync(context, token);
			}
			var fromProcess = GetExecutionIdFromProcess();
			if (!string.IsNullOrWhiteSpace(fromProcess))
			{
				_logger.LogInformation($"Using GlobalExecutionId '{fromProcess}' that was found in the execution process.");
				AddToContext(context, fromProcess);
				return Next.InvokeAsync(context, token);
			}
			var created = CreateExecutionId(context);
			AddToContext(context, created);
			_logger.LogInformation($"Creating new GlobalExecutionId '{created}' for this execution.");
			return Next.InvokeAsync(context, token);
		}

		protected virtual void AddToContext(IPipeContext context, string globalMessageId)
		{
			SaveInContext(context, globalMessageId);
		}

		protected virtual string CreateExecutionId(IPipeContext context)
		{
			return  Guid.NewGuid().ToString();
		}

		protected virtual string GetExecutionIdFromProcess()
		{
			return GlobalExecutionIdRepository.Get();
		}

		protected virtual string GetExecutionIdFromContext(IPipeContext context)
		{
			return ExecutionIdFunc(context);
		}

	}
}
