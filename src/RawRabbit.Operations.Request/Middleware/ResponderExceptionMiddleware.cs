using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponderExceptionOptions
	{
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<ExceptionInformation, IPipeContext, Task> HandlerFunc { get; set; }
		public Func<IPipeContext, Type> ResponseTypeFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	}

	public class ResponderExceptionMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;
		protected Func<IPipeContext, object> ExceptionInfoFunc;
		protected Func<ExceptionInformation, IPipeContext, Task> HandlerFunc;
		private readonly ILog _logger = LogProvider.For<ResponderExceptionMiddleware>();
		protected Func<IPipeContext, Type> ResponseTypeFunc;
		private Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgFunc;

		public ResponderExceptionMiddleware(ISerializer serializer, ResponderExceptionOptions options = null)
		{
			_serializer = serializer;
			ExceptionInfoFunc = options?.MessageFunc ?? (context => context.GetResponseMessage());
			HandlerFunc = options?.HandlerFunc;
			_deliveryArgFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			ResponseTypeFunc = options?.ResponseTypeFunc ?? (context =>
			{
				var type = GetDeliverEventArgs(context)?.BasicProperties.Type;
				return !string.IsNullOrWhiteSpace(type) ? Type.GetType(type, false) : typeof(object);
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var responseType = GetResponseType(context);
			if (responseType == typeof(ExceptionInformation))
			{
				var exceptionInfo = GetExceptionInfo(context);
				return HandleRespondException(exceptionInfo, context);
			}
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicDeliverEventArgs GetDeliverEventArgs(IPipeContext context)
		{
			return _deliveryArgFunc?.Invoke(context);
		}

		protected virtual Type GetResponseType(IPipeContext context)
		{
			return ResponseTypeFunc?.Invoke(context);
		}

		protected virtual string GetSerializedBody(IPipeContext context)
		{
			var deliveryArgs = GetDeliverEventArgs(context);
			var serialized = Encoding.UTF8.GetString(deliveryArgs?.Body ?? new byte[0]);
			return serialized;
		}

		protected virtual ExceptionInformation GetExceptionInfo(IPipeContext context)
		{
			var serialized = GetSerializedBody(context);
			return _serializer.Deserialize<ExceptionInformation>(serialized);
		}

		protected virtual Task HandleRespondException(ExceptionInformation exceptionInfo, IPipeContext context)
		{
			_logger.Info("An unhandled exception occured when remote tried to handle request.\n  Message: {exceptionMessage}\n  Stack Trace: {stackTrace}", exceptionInfo.Message, exceptionInfo.StackTrace);

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
