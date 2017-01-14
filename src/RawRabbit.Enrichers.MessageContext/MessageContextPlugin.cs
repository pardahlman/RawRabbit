using System;
using RawRabbit.Common;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext
{
	public static class MessageContextPlugin
	{
		public static IClientBuilder UseMessageContext<TMessageContext>(this IClientBuilder builder)
			where TMessageContext : new()
		{
			return UseMessageContext(builder, context => new TMessageContext());
		}

		public static IClientBuilder UseMessageContext<TMessageContext>(this IClientBuilder builder, Func<IPipeContext, TMessageContext> createFunc)
		{
			Func<IPipeContext, object> genericCreateFunc = context => createFunc(context);
			builder.Register(pipe => pipe.Use<HeaderSerializationMiddleware>(new HeaderSerializationOptions
			{
				HeaderKey = PropertyHeaders.Context,
				RetrieveItemFunc = context => context.GetMessageContext(),
				CreateItemFunc = genericCreateFunc
			}));
			return builder;
		}
	}
}
