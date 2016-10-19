using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public abstract class Middleware
	{
		public Middleware Next { get; set; }
		public abstract Task InvokeAsync(IPipeContext context);
	}
}