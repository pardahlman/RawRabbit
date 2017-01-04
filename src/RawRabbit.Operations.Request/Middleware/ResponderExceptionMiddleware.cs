using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponderExceptionOptions
	{
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<ExceptionInformation, IPipeContext, Task> HandlerFunc { get; set; }
	}

	public class ResponderExceptionMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, object> MessageFunc;
		protected Func<ExceptionInformation, IPipeContext, Task> HandlerFunc;
		private readonly ILogger _logger = LogManager.GetLogger<ResponderExceptionMiddleware>();

		public ResponderExceptionMiddleware(ResponderExceptionOptions options = null)
		{
			MessageFunc = options?.MessageFunc ?? (context => context.GetResponseMessage());
			HandlerFunc = options?.HandlerFunc;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var message = GetResponseMessage(context);
			if (message is ExceptionInformation)
			{
				return HandleRespondException(message as ExceptionInformation, context);
			}
			return Next.InvokeAsync(context, token);
		}

		protected virtual object GetResponseMessage(IPipeContext context)
		{
			return MessageFunc(context);
		}

		protected virtual Task HandleRespondException(ExceptionInformation exceptionInfo, IPipeContext context)
		{
			_logger.LogInformation($"An unhandled exception occured when remote tried to handle request.\n  Message: {exceptionInfo.Message}\n  Stack Trace: {exceptionInfo.StackTrace}");

			if (HandlerFunc != null)
			{
				return HandlerFunc(exceptionInfo, context);
			}

			var exception = new MessageHandlerException(exceptionInfo.Message)
			{
				InnerExceptionType = exceptionInfo.ExceptionType,
				InnerStackTrace = exceptionInfo.StackTrace,
				InnerMessage = exceptionInfo.InnerMessage
			};
			return TaskUtil.FromException(exception);
		}
	}
}
