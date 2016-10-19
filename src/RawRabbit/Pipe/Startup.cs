using RawRabbit.Context;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe
{
	public interface IStartup
	{
		void ConfigureSubscribe(IPipeBuilder pipe);
		void ConfigurePublish(IPipeBuilder pipe);
	}

	public class Startup<TMessageContext> : IStartup where TMessageContext : IMessageContext
	{
		public void ConfigureSubscribe(IPipeBuilder pipe)
		{
			pipe
				.Use<OperationConfigurationMiddleware>()
				.Use<RoutingKeyMiddleware>()
				.Use<QueueBindMiddleware>()
				.Use<MessageConsumeMiddleware>(new ConsumeOptions
				{
					Pipe = consume => consume
						.Use<MessageDeserializationMiddleware>()
						.Use<ContextExtractionMiddleware<TMessageContext>>()
						.Use<MessageChainingMiddleware>()
						.Use<MessageContextEnhanceMiddleware>()
						.Use<MessageInvokationMiddleware>()
				});
		}

		public void ConfigurePublish(IPipeBuilder pipe)
		{
			pipe
				.Use<OperationConfigurationMiddleware>()
				.Use<MessageChainingMiddleware>()
				.Use<GlobalMessageIdMiddleware>()
				.Use<RoutingKeyMiddleware>()
				.Use<ExchangeDeclareMiddleware>()
				.Use<MessageContextCreationMiddleware<TMessageContext>>()
				.Use<ContextSerializationMiddleware>()
				.Use<BasicPropertiesMiddleware>()
				.Use<MessageSerializationMiddleware>()
				.Use<PublishChannelMiddleware>()
				.Use<PublishAcknowledgeMiddleware>()
				.Use<PublishMessage>();
		}
	}
}
