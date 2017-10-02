using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class BasicConsumeExtension
	{
		public static Task BasicConsumeAsync(this IBusClient busClient, Func<BasicDeliverEventArgs, Task<Acknowledgement>> consumeFunc,
			Action<IPipeContext> context)
		{
			Func<object[], Task> genericFunc = args => consumeFunc(args[0] as BasicDeliverEventArgs);

			return busClient
				.InvokeAsync(pipe =>
					pipe
						.Use<ConsumeConfigurationMiddleware>()
						.Use<ExchangeDeclareMiddleware>()
						.Use<QueueDeclareMiddleware>()
						.Use<QueueBindMiddleware>(new QueueBindOptions
						{
							ExchangeNameFunc = ctx => ctx.GetConsumeConfiguration()?.ExchangeName
						})
						.Use<ConsumerCreationMiddleware>()
						.Use<ConsumerMessageHandlerMiddleware>(new ConsumeOptions
						{
							Pipe = p => p
								.Use<HandlerInvocationMiddleware>(new HandlerInvocationOptions
								{
									HandlerArgsFunc = ctx => new object[] { ctx.GetDeliveryEventArgs()},
								})
								.Use<ExplicitAckMiddleware>()
						})
						.Use<ConsumerConsumeMiddleware>(),
					ctx =>
					{
						ctx.Properties.Add(PipeKey.MessageHandler, genericFunc);
						context?.Invoke(ctx);
					}
				);
		}
	}
}
