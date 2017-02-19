using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Serilog.Context;
using Serilog.Core;

namespace RawRabbit.Logging.Serilog
{
	public class LogContextOptions
	{
		public Func<IPipeContext, ILogEventEnricher[]> LogEnricherFunc { get; set; }
	}

	public class LogContextMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, ILogEventEnricher[]> LogEnricherFunc;
		public override string StageMarker => Pipe.StageMarker.MessageDeserialized;

		public LogContextMiddleware(LogContextOptions options = null)
		{
			LogEnricherFunc = context => context.GetLogEventEnrichers() ?? options?.LogEnricherFunc?.Invoke(context);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var enrichers = GetLogEventEnrichers(context);
			using (LogContext.PushProperties(enrichers))
			{
				await Next.InvokeAsync(context, token);
			}
		}

		protected virtual ILogEventEnricher[] GetLogEventEnrichers(IPipeContext context)
		{
			return LogEnricherFunc?.Invoke(context);
		}
	}

	public static class LogContextPipeExtensions
	{
		private const string LogEventEnrichers = "Serilog:LogEventEnrichers";

		public static ILogEventEnricher[] GetLogEventEnrichers(this IPipeContext context)
		{
			return context.Get<ILogEventEnricher[]>(LogEventEnrichers);
		}

		public static IPipeContext UseLogEventEnrichers(this IPipeContext context, params ILogEventEnricher[] enrichers)
		{
			context.Properties.AddOrReplace(LogEventEnrichers, enrichers.ToArray());
			return context;
		}
	}
}
