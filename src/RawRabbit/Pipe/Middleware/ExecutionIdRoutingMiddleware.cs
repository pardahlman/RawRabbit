using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class ExecutionIdRoutingOptions
	{
		public Func<IPipeContext, bool> EnableRoutingFunc { get; set; }
		public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
		public Action<IPipeContext, string> UpdateAction { get; set; }
	}

	public class ExecutionIdRoutingMiddleware : Middleware
	{
		protected Func<IPipeContext, bool> EnableRoutingFunc;
		protected Func<IPipeContext, string> ExecutionIdFunc;
		protected Action<IPipeContext, string> UpdateAction;

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
					return;
				}
				var consumeConfig = context.GetConsumeConfiguration();
				if (consumeConfig != null)
				{
					consumeConfig.RoutingKey = $"{consumeConfig.RoutingKey}.#";
				}
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var enabled = GetRoutingEnabled(context);
			if (!enabled)
			{
				return Next.InvokeAsync(context, token);
			}
			var executionId = GetExecutionId(context);
			UpdateRoutingKey(context, executionId);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void UpdateRoutingKey(IPipeContext context, string executionId)
		{
			UpdateAction(context, executionId);
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
