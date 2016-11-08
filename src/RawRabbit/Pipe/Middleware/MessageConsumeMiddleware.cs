using System;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeOptions
	{
		public Action<IPipeBuilder> Pipe { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
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
		private readonly Middleware _consumePipe;
		private readonly Func<IPipeContext, IBasicConsumer> _consumeFunc;

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions consumeOptions)
		{
			_contextFactory = contextFactory;
			_consumeFunc = consumeOptions.ConsumerFunc ?? (context =>context.GetConsumer());
			_consumePipe = pipeBuilderFactory.Create(consumeOptions.Pipe);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumer = _consumeFunc(context);

			consumer.OnMessage((sender, args) =>
			{
				Task.Run(() =>
				{
					var consumeContext = _contextFactory.CreateContext(context.Properties.ToArray());
					consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
					return _consumePipe.InvokeAsync(consumeContext);
				});
			});

			return Next.InvokeAsync(context);
		}
	}
}
