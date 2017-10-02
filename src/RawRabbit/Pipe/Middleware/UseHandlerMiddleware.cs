using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class UseHandlerMiddleware : Middleware
	{
		private readonly Func<IPipeContext, Func<Task>, Task> _handler;

		public UseHandlerMiddleware(Func<IPipeContext, Func<Task>, Task> handler)
		{
			_handler = handler;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			await  _handler(context, () => Next.InvokeAsync(context, token));
		}
	}
}