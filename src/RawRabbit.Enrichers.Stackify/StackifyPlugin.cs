using System;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class StackifyPlugin
	{
		public static IClientBuilder UseStackify(this IClientBuilder builder, Action<IPipeContext> action)
		{
			return UseStackify(builder);
		}

		public static IClientBuilder UseStackify(this IClientBuilder builder)
		{
			builder.Register(
				pipe => pipe
					.Replace<HandlerInvocationMiddleware, RawRabbit.Enrichers.Stackify.Middleware.StackifyOperationMiddleware>(argsFunc: oldArgs => oldArgs));
			return builder;
		}
	}
}
