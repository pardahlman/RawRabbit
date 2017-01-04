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
		public Func<IPipeContext, SemaphoreSlim> SemaphoreFunc { get; set; }
	}

	public class MessageConsumeMiddleware : Middleware
	{
		protected IPipeContextFactory ContextFactory;
		protected Middleware ConsumePipe;
		protected Func<IPipeContext, IBasicConsumer> ConsumeFunc;
		protected Func<IPipeContext, SemaphoreSlim> SemaphoreFunc;
		private readonly ILogger _logger = LogManager.GetLogger<MessageConsumeMiddleware>();

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions options = null)
		{
			ContextFactory = contextFactory;
			ConsumeFunc = options?.ConsumerFunc ?? (context =>context.GetConsumer());
			ConsumePipe = pipeBuilderFactory.Create(options?.Pipe ?? (builder => {}));
			SemaphoreFunc = options?.SemaphoreFunc ?? (context => context.Get<SemaphoreSlim>(PipeKey.ConsumeSemaphore));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var consumer = ConsumeFunc(context);
			var semaphore = GetOrCreateSemaphore(context);
			consumer.OnMessage((sender, args) =>
			{
				ThrottledExecution(() => InvokeConsumePipeAsync(context, args), semaphore, token);
			});

			return Next.InvokeAsync(context, token);
		}

		protected virtual SemaphoreSlim GetOrCreateSemaphore(IPipeContext context)
		{
			return SemaphoreFunc(context);
		}

		protected virtual Task InvokeConsumePipeAsync(IPipeContext context, BasicDeliverEventArgs args)
		{
			var consumeContext = ContextFactory.CreateContext(context.Properties.ToArray());
			consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
			return ConsumePipe.InvokeAsync(consumeContext);
		}

		protected virtual void ThrottledExecution(Func<Task> asyncAction, SemaphoreSlim semaphore, CancellationToken ct)
		{
			if (semaphore == null)
			{
				_logger.LogDebug("Consuming messages without throttle.");
				Task.Run(asyncAction, ct);
				return;
			}
			semaphore
				.WaitAsync(ct)
				.ContinueWith(tEnter =>
				{
					try
					{
						Task.Run(asyncAction, ct)
							.ContinueWith(tDone => semaphore.Release(), ct);
					}
					catch (Exception e)
					{
						_logger.LogError("An unhandled exception was thrown when consuming message", e);
						semaphore.Release();
					}
				}, ct);
		}
	}
}
