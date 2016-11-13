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
		public static IClientBuilder PublishMessageContext<TMessageContext>(this IClientBuilder builder, Func<IPipeContext, TMessageContext> createFunc = null)
			where TMessageContext : IMessageContext, new()
		{
			Func<IPipeContext, object> genericCreateFunc;
			if (createFunc == null)
			{
				genericCreateFunc = context =>
				{
					var msgContext = new TMessageContext
					{
						GlobalRequestId = Guid.NewGuid()
					};
					context.Properties.TryAdd(PipeKey.MessageContext, msgContext);
					return msgContext;
				};
			}
			else
			{
				genericCreateFunc = context => createFunc(context);
			}
			builder.Register(pipe => pipe.Use<HeaderSerializationMiddleware>(new HeaderSerializationOptions
			{
				ExecutePredicate = context => string.Equals(context.Get<string>(PipeKey.Operation), PublishKey.Publish),
				HeaderKey = PropertyHeaders.Context,
				RetrieveItemFunc = context => context.GetMessageContext(),
				CreateItemFunc = genericCreateFunc
			}));
			return builder;
		}
	}
}
