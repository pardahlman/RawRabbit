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

	public class ConsumerMessageHandlerMiddleware : Middleware
	{
		protected IPipeContextFactory ContextFactory;
		protected Middleware ConsumePipe;
		protected Func<IPipeContext, IBasicConsumer> ConsumeFunc;
		protected Func<IPipeContext, SemaphoreSlim> SemaphoreFunc;
		protected Func<IPipeContext, Action<Func<Task>, CancellationToken>> ThrottledExecutionFunc;
		private readonly ILogger _logger = LogManager.GetLogger<ConsumerMessageHandlerMiddleware>();

		public ConsumerMessageHandlerMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions options = null)
		{
			ContextFactory = contextFactory;
			ConsumeFunc = options?.ConsumerFunc ?? (context =>context.GetConsumer());
			ConsumePipe = pipeBuilderFactory.Create(options?.Pipe ?? (builder => {}));
			ThrottledExecutionFunc = options?.ThrottleFuncFunc ?? (context => context.GetConsumeThrottleAction());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = ConsumeFunc(context);
			var throttlingFunc = GetThrottlingFunc(context);
			consumer.OnMessage((sender, args) =>
			{
				throttlingFunc(() => InvokeConsumePipeAsync(context, args, token), token);
			});

			await Next.InvokeAsync(context, token);
		}

		private Action<Func<Task>, CancellationToken> GetThrottlingFunc(IPipeContext context)
		{
			return ThrottledExecutionFunc(context);
		}

		protected virtual async Task InvokeConsumePipeAsync(IPipeContext context, BasicDeliverEventArgs args, CancellationToken token)
		{
			_logger.LogDebug($"Invoking consumer pipe for message '{args?.BasicProperties?.MessageId}'.");
			var consumeContext = ContextFactory.CreateContext(context.Properties.ToArray());
			consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
			try
			{
				await ConsumePipe.InvokeAsync(consumeContext, token);
			}
			catch (Exception e)
			{
				_logger.LogError($"An unhandled exception was thrown when consuming message with routing key {args.RoutingKey}", e);
				throw;
			}
		}
	}
}
