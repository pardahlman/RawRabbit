using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Publish.Middleware;
using RawRabbit.Instantiation;

namespace RawRabbit
{
	public static class MessageContextPlugin
	{
		public static IClientBuilder PublishMessageContext<TMessageContext>(this IClientBuilder builder) where TMessageContext : IMessageContext, new()
		{
			builder.Register(pipe => pipe.Use<MessageContextMiddleware<TMessageContext>>());
			return builder;
		}
	}
}
