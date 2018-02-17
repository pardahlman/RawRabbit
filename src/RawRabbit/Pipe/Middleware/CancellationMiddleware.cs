using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class CancellationMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			token.ThrowIfCancellationRequested();
			return Next.InvokeAsync(context, token);
		}
	}
}
