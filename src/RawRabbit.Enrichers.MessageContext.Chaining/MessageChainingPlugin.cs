using RawRabbit.Enrichers.MessageContext.Chaining.Dependencies;
using RawRabbit.Enrichers.MessageContext.Chaining.Middleware;
using RawRabbit.Instantiation;

namespace RawRabbit
{
	public static class MessageChainingPlugin
	{
		public static IClientBuilder UseMessageChaining(this IClientBuilder builder)
		{
			builder.Register(
				pipe => pipe
					.Use<PublishChainingMiddleware>()
					.Use<ConsumeChainingMiddleware>(),
				ioc => ioc
					.AddSingleton<IMessageContextRepository, MessageContextRepository>());
			return builder;
		}
	}
}
