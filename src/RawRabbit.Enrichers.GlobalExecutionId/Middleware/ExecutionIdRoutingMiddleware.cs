using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class ExecutionIdRoutingOptions
	{
		public Func<IPipeContext, bool> EnableRoutingFunc { get; set; }
		public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
		public Func<IPipeContext, string, string> UpdateAction { get; set; }
	}

	public class ExecutionIdRoutingMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.PublishConfigured;
		protected Func<IPipeContext, bool> EnableRoutingFunc;
		protected Func<IPipeContext, string> ExecutionIdFunc;
		protected Func<IPipeContext, string, string> UpdateAction;
		private readonly ILogger _logger = LogManager.GetLogger<ExecutionIdRoutingMiddleware>();

		public ExecutionIdRoutingMiddleware(ExecutionIdRoutingOptions options = null)
		{
			EnableRoutingFunc = options?.EnableRoutingFunc ?? (c => c.GetClientConfiguration()?.RouteWithGlobalId ?? false);
			ExecutionIdFunc = options?.ExecutionIdFunc ?? (c => c.GetGlobalExecutionId());
			UpdateAction = options?.UpdateAction ?? ((context, executionId) =>
			{
				var pubConfig = context.GetBasicPublishConfiguration();
				if (pubConfig != null)
				{
					pubConfig.RoutingKey = $"{pubConfig.RoutingKey}.{executionId}";
					return pubConfig.RoutingKey;
				}
				return string.Empty;
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var enabled = GetRoutingEnabled(context);
			if (!enabled)
			{
				_logger.LogDebug("Routing with GlobalExecutionId disabled.");
				return Next.InvokeAsync(context, token);
			}
			var executionId = GetExecutionId(context);
			UpdateRoutingKey(context, executionId);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void UpdateRoutingKey(IPipeContext context, string executionId)
		{
			_logger.LogDebug($"Updating routing key with GlobalExecutionId '{executionId}'");
			var updated = UpdateAction(context, executionId);
			_logger.LogInformation($"Routing key updated with GlobalExecutionId: {updated}.");
		}

		protected virtual bool GetRoutingEnabled(IPipeContext pipeContext)
		{
			return EnableRoutingFunc(pipeContext);
		}

		protected virtual string GetExecutionId(IPipeContext context)
		{
			return ExecutionIdFunc(context);
		}
	}
}
