using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.HttpContext
{
	public class NetFxHttpContextMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.Initialized;

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
#if net451
			context.UseHttpContext(System.Web.HttpContext.Current);
#endif
			return Next.InvokeAsync(context, token);
		}
	}
}
