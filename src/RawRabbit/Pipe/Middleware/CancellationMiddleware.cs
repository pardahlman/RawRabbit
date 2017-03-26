using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class CancellationMiddleware : Middleware
	{
		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			token.ThrowIfCancellationRequested();
			await Next.InvokeAsync(context, token);
		}
	}
}
