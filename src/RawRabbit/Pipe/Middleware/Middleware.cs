using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public abstract class Middleware
	{
		public Middleware Next { get; set; }
		public abstract Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken));
	}
}