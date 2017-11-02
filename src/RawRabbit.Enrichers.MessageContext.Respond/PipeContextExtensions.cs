using System;
using RawRabbit.Operations.Respond.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Respond
{
	public static class PipeContextExtensions
	{
		public const string PipebasedContextFunc = "Respond:MessageContext:PipebasedContext";
		private const string MessageContextType = "Respond:MessageContext:Type";

		public static IPipeContext AddMessageContextType<TMessageContext>(this IPipeContext context)
		{
			context.Properties.TryAdd(MessageContextType, typeof(TMessageContext));
			return context;
		}

		public static Type GetMessageContextType(this IPipeContext context)
		{
			return context.Get(MessageContextType, typeof(object));
		}

		public static IRespondContext UseMessageContext(this IRespondContext context, Func<IPipeContext, object> contextFunc)
		{
			context.Properties.TryAdd(PipebasedContextFunc, contextFunc);
			return context;
		}

		public static Func<IPipeContext, object> GetMessageContextResolver(this IPipeContext context)
		{
			return context.Get<Func<IPipeContext, object>>(PipebasedContextFunc);
		}
	}
}
