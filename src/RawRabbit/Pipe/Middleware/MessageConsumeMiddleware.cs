using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeOptions
	{
		public Action<IPipeBuilder> Pipe { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, Action<Func<Task>, CancellationToken>> ThrottleFuncFunc { get; set; }
	}

	public class MessageConsumeMiddleware : Middleware
	{
		protected IPipeContextFactory ContextFactory;
		protected Middleware ConsumePipe;
		protected Func<IPipeContext, IBasicConsumer> ConsumeFunc;
		protected Func<IPipeContext, SemaphoreSlim> SemaphoreFunc;
		protected Func<IPipeContext, Action<Func<Task>, CancellationToken>> ThrottledExecutionFunc;
		private readonly ILogger _logger = LogManager.GetLogger<MessageConsumeMiddleware>();

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions options = null)
		{
			ContextFactory = contextFactory;
			ConsumeFunc = options?.ConsumerFunc ?? (context =>context.GetConsumer());
			ConsumePipe = pipeBuilderFactory.Create(options?.Pipe ?? (builder => {}));
			ThrottledExecutionFunc = options?.ThrottleFuncFunc ?? (context => context.GetConsumeThrottleAction());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = ConsumeFunc(context);
			var throttlingFunc = GetThrottlingFunc(context);
			consumer.OnMessage((sender, args) =>
			{
				throttlingFunc(() => InvokeConsumePipeAsync(context, args, token), token);
			});

			return Next.InvokeAsync(context, token);
		}

		private Action<Func<Task>, CancellationToken> GetThrottlingFunc(IPipeContext context)
		{
			return ThrottledExecutionFunc(context);
		}

		protected virtual Task InvokeConsumePipeAsync(IPipeContext context, BasicDeliverEventArgs args, CancellationToken token)
		{
			_logger.LogDebug($"Invoking consumer pipe for message '{args?.BasicProperties?.MessageId}'.");
			var consumeContext = ContextFactory.CreateContext(context.Properties.ToArray());
			consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
			return ConsumePipe.InvokeAsync(consumeContext, token);
		}
	}
}
