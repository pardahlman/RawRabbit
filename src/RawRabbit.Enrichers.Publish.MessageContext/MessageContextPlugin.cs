using RawRabbit.Context;
using RawRabbit.Enrichers.Publish.MessageContext.Middleware;
using RawRabbit.vNext.Pipe;

namespace RawRabbit.Enrichers.Publish.MessageContext
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
