using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Client
{
	public static class MessagePublishExtension
	{
		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublishConfigurationBuilder> config = null)
		{
			return client.InvokeAsync(pipe => pipe
				.Use((context, next) =>
				{
					context.Properties.Add(PipeKey.Operation, Operation.Publish);
					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.Message, message);
					context.Properties.Add(PipeKey.ConfigurationAction, config);
					return next();
				})
				.Use<OperationConfigurationMiddleware>()
				.Use<MessageChainingMiddleware>()
				.Use<GlobalMessageIdMiddleware>()
				.Use<RoutingKeyMiddleware>()
				.Use<ExchangeDeclareMiddleware>()
				.Use<MessageContextCreationMiddleware<MessageContext>>()
				.Use<ContextSerializationMiddleware>()
				.Use<BasicPropertiesMiddleware>()
				.Use<MessageSerializationMiddleware>()
				.Use<PublishChannelMiddleware>()
				.Use<PublishAcknowledgeMiddleware>()
				.Use<PublishMessage>());
		}
	}
}