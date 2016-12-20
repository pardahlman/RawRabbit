using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class CancellationMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			if (token.IsCancellationRequested)
			{
				return TaskUtil.FromCancelled();
			}
			return Next.InvokeAsync(context, token);
		}
	}
}
