using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class NoOpMiddleware : Middleware
	{
		
		public override Task InvokeAsync(IPipeContext context)
		{
			return Task.FromResult(0);
		}
	}
}