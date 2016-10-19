using System;
using System.Threading.Tasks;
using RawRabbit.Configuration;

namespace RawRabbit.Pipe.Middleware
{
	public class UseHandlerMiddleware : Middleware
	{
		private readonly Func<IPipeContext, Func<Task>, Task> _handler;

		public UseHandlerMiddleware(Func<IPipeContext, Func<Task>, Task> handler)
		{
			_handler = handler;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return _handler(context, () => Next.InvokeAsync(context));
		}
	}
}