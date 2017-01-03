using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Action<IModel, BasicDeliverEventArgs, ConsumeConfiguration> AckOrNackFunc { set; get; }
		public Action<IPipeContext, ExceptionInformation> SaveAction { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class RespondExceptionMiddleware : ExceptionHandlingMiddleware
	{
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, ExceptionInformation> SaveAction;
		protected Action<IModel, BasicDeliverEventArgs, ConsumeConfiguration> AckOrNackFunc;

		public RespondExceptionMiddleware(IPipeBuilderFactory factory, RespondExceptionOptions options = null)
			: base(factory, new ExceptionHandlingOptions { InnerPipe = options?.InnerPipe })
		{
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			ConsumeConfigFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetConsumer()?.Model);
			AckOrNackFunc = options?.AckOrNackFunc;
			SaveAction = options?.SaveAction ?? ((context, information) => context.Properties.TryAdd(RespondKey.ResponseMessage, information));
		}

		protected override Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			var innerException = UnwrapInnerException(exception);
			var args = GetDeliveryArgs(context);
			var cfg = GetConsumeConfiguration(context);
			var channel = GetChannel(context);
			AckOrNack(channel, args, cfg);
			var exceptionInfo = CreateExceptionInformation(innerException, args, cfg, context);
			SaveInConteext(context, exceptionInfo);
			return Next.InvokeAsync(context, token);
		}

		private void AckOrNack(IModel channel, BasicDeliverEventArgs args, ConsumeConfiguration cfg)
		{
			if (AckOrNackFunc != null)
			{
				AckOrNackFunc(channel, args, cfg);
				return;
			}
			if (cfg.NoAck)
			{
				return;
			}
			channel.BasicAck(args.DeliveryTag, false);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			return DeliveryArgsFunc(context);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
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

		protected virtual void SaveInConteext(IPipeContext context, ExceptionInformation info)
		{
			SaveAction?.Invoke(context, info);
		}
	}
}
