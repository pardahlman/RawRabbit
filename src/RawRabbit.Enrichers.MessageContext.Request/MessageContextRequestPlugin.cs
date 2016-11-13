using System;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class MessageContextRequestPlugin
	{
		public static IClientBuilder RequestMessageContext<TMessageContext>(this IClientBuilder builder) where TMessageContext : IMessageContext, new()
		{
			builder.Register(p => p.Use<HeaderSerializationMiddleware>(new HeaderSerializationOptions
			{
				HeaderKey = PropertyHeaders.Context,
				RetrieveItemFunc = context =>
				{
					var msgCtx = context.GetMessageContext();
					context.Properties.TryAdd(PipeKey.MessageContext, msgCtx);
					return msgCtx;
				},
				CreateItemFunc = context => new TMessageContext
				{
					GlobalRequestId = Guid.NewGuid()
				}
			}));
			return builder;
		}
	}
}
