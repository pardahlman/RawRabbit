using System;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeOptions
	{
		public Action<IPipeBuilder> Pipe { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, bool> SynchronousExecutionFunc { get; set; }
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
		private Func<IPipeContext, bool> _synchronousExecutionFunc;

		public MessageConsumeMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions consumeOptions)
		{
			_contextFactory = contextFactory;
			_consumeFunc = consumeOptions.ConsumerFunc ?? (context =>context.GetConsumer());
			_consumePipe = pipeBuilderFactory.Create(consumeOptions.Pipe);
			_synchronousExecutionFunc = consumeOptions?.SynchronousExecutionFunc ?? (context => false);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var consumer = _consumeFunc(context);

			consumer.OnMessage((sender, args) =>
			{
				var sync = _synchronousExecutionFunc(context);
				if (sync)
				{
					InvokeConsumePipeAsync(context, args)
						.GetAwaiter()
						.GetResult();
				}
				else
				{
					Task.Run(() => InvokeConsumePipeAsync(context, args));
				}
			});

			return Next.InvokeAsync(context);
		}

		protected Task InvokeConsumePipeAsync(IPipeContext context, BasicDeliverEventArgs args)
		{
			var consumeContext = _contextFactory.CreateContext(context.Properties.ToArray());
			consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
			return _consumePipe.InvokeAsync(consumeContext);
		}
	}
}
