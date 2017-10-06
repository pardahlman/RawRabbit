using System;
using RabbitMQ.Client.Events;
using RawRabbit.Operations.Subscribe.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe
{
	public static class PipeContextExtensions
	{
		public const string PipebasedContextFunc = "Subscribe:MessageContext:PipebasedContext";
		private const string MessageContextType = "Subscribe:MessageContext:Type";

		public static ISubscribeContext UseMessageContext(this ISubscribeContext context, Func<IPipeContext, object> contextFunc)
		{
			context.Properties.TryAdd(PipebasedContextFunc, contextFunc);
			return context;
		}

		public static IPipeContext AddMessageContextType<TMessageContext>(this IPipeContext context)
		{
			context.Properties.TryAdd(MessageContextType, typeof(TMessageContext));
			return context;
		}

		public static Func<IPipeContext, object> GetMessageContextResolver(this IPipeContext context)
		{
			return context.Get<Func<IPipeContext, object>>(PipebasedContextFunc);
		}

		public static Type GetMessageContextType(this IPipeContext context)
		{
			return context.Get(MessageContextType, typeof(object));
		}
	}
}
