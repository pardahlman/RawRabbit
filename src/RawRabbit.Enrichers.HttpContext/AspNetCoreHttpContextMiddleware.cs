using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.HttpContext
{
	public class AspNetCoreHttpContextMiddleware : StagedMiddleware
	{
#if NETSTANDARD1_6
		private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpAccessor;

		public AspNetCoreHttpContextMiddleware(Microsoft.AspNetCore.Http.IHttpContextAccessor httpAccessor)
		{
			_httpAccessor = httpAccessor;
		}
#endif
		public override string StageMarker => Pipe.StageMarker.Initialized;

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
#if NETSTANDARD1_6
			context.UseHttpContext(_httpAccessor.HttpContext);
#endif
			return Next.InvokeAsync(context, token);
		}

	}
}
