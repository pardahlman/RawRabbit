using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class ExceptionHandlingOptions
	{
		public Func<Exception, IPipeContext, CancellationToken, Task> HandlingFunc { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class ExceptionHandlingMiddleware : Middleware
	{
		protected Func<Exception, IPipeContext, CancellationToken, Task> HandlingFunc;
		public Middleware InnerPipe;

		public ExceptionHandlingMiddleware(IPipeBuilderFactory factory, ExceptionHandlingOptions options = null)
		{
			HandlingFunc = options?.HandlingFunc ?? ((exception, context, token) => Task.FromResult(0));
			InnerPipe = factory.Create(options?.InnerPipe);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			try
			{
				return InnerPipe
					.InvokeAsync(context, token)
					.ContinueWith(t => t.IsFaulted
						? OnExceptionAsync(t.Exception, context, token)
						: Next.InvokeAsync(context, token), token)
					.Unwrap();
			}
			catch (Exception e)
			{
				return OnExceptionAsync(e, context, token);
			}
		}

		protected virtual Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			return HandlingFunc(exception, context, token);
		}

		protected static Exception UnwrapInnerException(Exception exception)
		{
			if (exception is AggregateException && exception.InnerException != null)
			{
				return UnwrapInnerException(exception.InnerException);
			}
			return exception;
		}
	}
}
