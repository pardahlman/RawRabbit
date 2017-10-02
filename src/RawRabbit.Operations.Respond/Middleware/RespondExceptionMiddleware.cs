using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Exceptions;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondExceptionOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc { get; set; }
		public Action<IPipeContext, ExceptionInformation> SaveAction { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class RespondExceptionMiddleware : ExceptionHandlingMiddleware
	{
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, ExceptionInformation> SaveAction;

		public RespondExceptionMiddleware(IPipeBuilderFactory factory, RespondExceptionOptions options = null)
			: base(factory, new ExceptionHandlingOptions { InnerPipe = options?.InnerPipe })
		{
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumeConfigFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
			SaveAction = options?.SaveAction ?? ((context, information) => context.Properties.TryAdd(RespondKey.ResponseMessage, information));
		}

		protected override Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			var innerException = UnwrapInnerException(exception);
			var args = GetDeliveryArgs(context);
			var cfg = GetConsumeConfiguration(context);
			AddAcknowledgementToContext(context, cfg);
			var exceptionInfo = CreateExceptionInformation(innerException, args, cfg, context);
			SaveInContext(context, exceptionInfo);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void AddAcknowledgementToContext(IPipeContext context, ConsumeConfiguration cfg)
		{
			if (cfg.AutoAck)
			{
				return;
			}
			context.Properties.TryAdd(PipeKey.MessageHandlerResult, Task.FromResult<Common.Acknowledgement>(new Ack()));
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			return DeliveryArgsFunc(context);
		}

		protected virtual ConsumeConfiguration GetConsumeConfiguration(IPipeContext context)
		{
			return ConsumeConfigFunc(context);
		}

		protected virtual ExceptionInformation CreateExceptionInformation(Exception exception, BasicDeliverEventArgs args, ConsumeConfiguration cfg, IPipeContext context)
		{
			return new ExceptionInformation
			{
				Message = $"An unhandled exception was thrown when consuming a message\n  MessageId: {args.BasicProperties.MessageId}\n  Queue: '{cfg.QueueName}'\n  Exchange: '{cfg.ExchangeName}'\nSee inner exception for more details.",
				ExceptionType = exception.GetType().FullName,
				StackTrace = exception.StackTrace,
				InnerMessage = exception.Message
			};
		}

		protected virtual void SaveInContext(IPipeContext context, ExceptionInformation info)
		{
			SaveAction?.Invoke(context, info);
		}
	}
}
