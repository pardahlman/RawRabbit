using System;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeOptions
	{
		public Action<IPipeBuilder> Pipe { get; set; }
	}

	public static class PipeBuilderConsumeExtension
	{
		public static IPipeBuilder UseMessageConsume(this IPipeBuilder pipe, Action<IPipeBuilder> consumeBuilder)
		{
			return pipe.Use<MessageConsumeMiddleware>(new ConsumeOptions {Pipe = consumeBuilder});
		}
	}

	public class MessageConsumeMiddleware : Middleware
	{
		private readonly IPipeContextFactory _contextFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly Middleware _consumePipe;

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions consumeOptions, IConsumerFactory consumerFactory)
		{
			_contextFactory = contextFactory;
			_consumerFactory = consumerFactory;

			_consumePipe = pipeBuilderFactory.Create(consumeOptions.Pipe);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumerCfg = context.GetConsumerConfiguration();
			var channel = context.GetChannel();

			var consumer = _consumerFactory.CreateConsumer(consumerCfg, channel);
			context.Properties.Add(PipeKey.Consumer, consumer);
			consumer.OnMessageAsync = (o, args) =>
			{
				var consumeContext = _contextFactory.CreateContext(context.Properties.ToArray());
				consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
				return _consumePipe.InvokeAsync(consumeContext);
			};
			consumer.Model.BasicConsume(
				queue: consumerCfg.Queue.FullQueueName,
				noAck: consumerCfg.NoAck,
				consumer: consumer);
			return Next.InvokeAsync(context);
		}
	}
}
