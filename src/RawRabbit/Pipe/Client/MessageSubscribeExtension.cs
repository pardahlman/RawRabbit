using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Client
{
	public static class MessageSubscribeExtension
	{
		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null) where TMessageContext : IMessageContext
		{
			return client.InvokeAsync(pipe => pipe
				.Use((context, next) =>
				{
					Func<object, IMessageContext, Task> genericHandler = (o, c) => subscribeMethod((TMessage)o, (TMessageContext)c);

					context.Properties.Add(PipeKey.Operation, Operation.Subscribe);
					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context.Properties.Add(PipeKey.ConfigurationAction, configuration);
					return next();
				})
				.Use<OperationConfigurationMiddleware>()
				.Use<RoutingKeyMiddleware>()
				.Use<QueueBindMiddleware>()
				.Use<MessageConsumeMiddleware>(new ConsumeOptions
				{
					Pipe = consume => consume
						.Use<MessageDeserializationMiddleware>()
						.Use<ContextExtractionMiddleware<MessageContext>>()
						.Use<MessageChainingMiddleware>()
						.Use<MessageContextEnhanceMiddleware>()
						.Use<MessageInvokationMiddleware>()
				}));
		}
	}
}