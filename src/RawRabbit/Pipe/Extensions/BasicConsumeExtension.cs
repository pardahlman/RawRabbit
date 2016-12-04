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
			Action<IConsumerConfigurationBuilder> cfg)
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
							ExchangeNameFunc = context => context.GetConsumeConfiguration()?.ExchangeName
						})
						.Use<ConsumerCreationMiddleware>()
						.Use<MessageConsumeMiddleware>(new ConsumeOptions
						{
							Pipe = p => p
								.Use<MessageHandlerInvokationMiddleware>(new MessageHandlerInvokationOptions
								{
									HandlerArgsFunc = context => new object[] {context.GetDeliveryEventArgs()},
								})
								.Use<ExplicitAckMiddleware>()
						}),
					context =>
					{
						context.Properties.Add(PipeKey.MessageHandler, genericFunc);
						context.Properties.Add(PipeKey.ConfigurationAction, cfg);
					}
				);
		}
	}
}
