using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

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
		private readonly ILog _logger = LogProvider.For<ExceptionHandlingMiddleware>();

		public ExceptionHandlingMiddleware(IPipeBuilderFactory factory, ExceptionHandlingOptions options = null)
		{
			HandlingFunc = options?.HandlingFunc ?? ((exception, context, token) => Task.FromResult(0));
			InnerPipe = factory.Create(options?.InnerPipe);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			try
			{
				await InnerPipe.InvokeAsync(context, token);
				await Next.InvokeAsync(context, token);
			}
			catch (Exception e)
			{
				_logger.Error("Exception thrown. Will be handled by Exception Handler", e);
				await OnExceptionAsync(e, context, token);
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
