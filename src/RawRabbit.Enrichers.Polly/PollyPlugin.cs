using System;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class PollyPlugin
	{
		public static IClientBuilder UsePolly(this IClientBuilder builder, Action<IPipeContext> action)
		{
			return UsePolly(builder, new PolicyOptions {PolicyAction = action});
		}

		public static IClientBuilder UsePolly(this IClientBuilder builder, PolicyOptions options)
		{
			builder.Register(pipe => pipe
				.Use<PolicyMiddleware>(options)
				.Replace<QueueDeclareMiddleware, Enrichers.Polly.Middleware.QueueDeclareMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<ExchangeDeclareMiddleware, Enrichers.Polly.Middleware.ExchangeDeclareMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<QueueBindMiddleware, Enrichers.Polly.Middleware.QueueBindMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<ConsumerMiddleware, Enrichers.Polly.Middleware.ConsumerMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<BasicPublishMiddleware, Enrichers.Polly.Middleware.BasicPublishMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<ExplicitAckMiddleware, Enrichers.Polly.Middleware.ExplicitAckMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<TransientChannelMiddleware, Enrichers.Polly.Middleware.TransientChannelMiddleware>(argsFunc: oldArgs => oldArgs)
				.Replace<HandlerInvokationMiddleware, Enrichers.Polly.Middleware.HandlerInvokationMiddleware>(argsFunc: oldArgs => oldArgs)
			);
			return builder;
		}
	}
}
