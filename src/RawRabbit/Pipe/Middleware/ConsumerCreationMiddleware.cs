using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumerCreationOptions
	{
		public Func<IConsumerFactory, CancellationToken, IPipeContext, Task<IBasicConsumer>> ConsumerFunc { get; set; }
	}

	public class ConsumerCreationMiddleware : Middleware
	{
		protected IConsumerFactory ConsumerFactory;
		protected Func<IPipeContext, ConsumeConfiguration> ConfigFunc;
		protected Func<IConsumerFactory, CancellationToken, IPipeContext, Task<IBasicConsumer>> ConsumerFunc;
		private readonly ILogger _logger = LogManager.GetLogger<ConsumerCreationMiddleware>();

		public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)
		{
			ConsumerFactory = consumerFactory;
			ConsumerFunc = options?.ConsumerFunc ?? ((factory, token, context) => factory.CreateConsumerAsync(context.GetChannel(), token));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var consumer = await GetOrCreateConsumerAsync(context, token);
			context.Properties.TryAdd(PipeKey.Consumer, consumer);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task<IBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
		{
			var consumerTask = ConsumerFunc(ConsumerFactory, token, context);
			if (consumerTask == null)
			{
				_logger.LogWarning("No Consumer creation task found in Pipe context.");
			}
			return consumerTask;
		}
	}
}
