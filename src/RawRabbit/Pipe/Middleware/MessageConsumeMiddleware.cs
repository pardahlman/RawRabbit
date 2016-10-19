using System;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
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
		private readonly IChannelFactory _channelFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly Middleware _consumePipe;

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions consumeOptions, IChannelFactory channelFactory, IConsumerFactory consumerFactory)
		{
			_contextFactory = contextFactory;
			_channelFactory = channelFactory;
			_consumerFactory = consumerFactory;

			var builder = pipeBuilderFactory.Create();
			consumeOptions.Pipe(builder);
			_consumePipe = builder.Build();
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumerCfg = context.GetConsumerConfiguration();
			return _channelFactory
				.CreateChannelAsync()
				.ContinueWith(tChannel =>
					{
						var consumer = _consumerFactory.CreateConsumer(consumerCfg, tChannel.Result);
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
					})
				.Unwrap();
		}
	}
}
