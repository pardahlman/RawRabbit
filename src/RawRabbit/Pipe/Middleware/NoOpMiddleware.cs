using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class NoOpMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			return Task.FromResult(0);
		}
	}
}