using System;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Instantiation;
using RawRabbit.Operations.Publish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class MessageContextPlugin
	{
		public static IClientBuilder PublishMessageContext<TMessageContext>(this IClientBuilder builder)
			where TMessageContext : IMessageContext, new()
		{
			return PublishMessageContext(builder, context => new TMessageContext
			{
				GlobalRequestId = Guid.NewGuid()
			});
		}

		public static IClientBuilder PublishMessageContext<TMessageContext>(this IClientBuilder builder, Func<IPipeContext, TMessageContext> createFunc)
			where TMessageContext : IMessageContext
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
