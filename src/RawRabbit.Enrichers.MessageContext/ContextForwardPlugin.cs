using RawRabbit.Enrichers.MessageContext.Dependencies;
using RawRabbit.Enrichers.MessageContext.Middleware;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.MessageContext
{
	public static class ContextForwardPlugin
	{
		public static IClientBuilder UseContextForwarding(this IClientBuilder builder)
		{
			builder.Register(
				pipe => pipe
					.Use<PublishForwardingMiddleware>()
					.Use<ConsumeForwardingMiddleware>(),
				ioc => ioc
					.AddSingleton<IMessageContextRepository, MessageContextRepository>());
			return builder;
		}
	}
}
