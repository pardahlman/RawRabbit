using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Extensions
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
						.Use<ConsumerMiddleware>()
						.Use<MessageConsumeMiddleware>(new ConsumeOptions
						{
							Pipe = p => p
								.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
								{
									HandlerArgsFunc = ctx => new object[] { ctx.GetDeliveryEventArgs()},
								})
								.Use<ExplicitAckMiddleware>()
						}),
					ctx =>
					{
						ctx.Properties.Add(PipeKey.MessageHandler, genericFunc);
						context?.Invoke(ctx);
					}
				);
		}
	}
}
