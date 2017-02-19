using System;
using RawRabbit.Instantiation;
using RawRabbit.Logging.Serilog;
using RawRabbit.Pipe;
using Serilog.Core;

namespace RawRabbit
{
	public static class LogContextPlugin
	{
		public static IClientBuilder UseSerilogEnrichers(this IClientBuilder builder, Func<IPipeContext, ILogEventEnricher[]> enrichers)
		{
			builder.Register(pipe => pipe
				.Use<LogContextMiddleware>(new LogContextOptions
				{
					LogEnricherFunc =  enrichers
				}));
			return builder;
		}
	}
}
