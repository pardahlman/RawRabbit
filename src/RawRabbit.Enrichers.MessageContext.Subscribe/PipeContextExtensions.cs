using System;
using RabbitMQ.Client.Events;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe
{
	public static class PipeContextExtensions
	{
		private const string PipebasedContextFunc = "PipebasedContextFunc";
		private const string MessageContextType = "Subscribe:MessageContext:Type";

		public static IPipeContext UseMessageContext(this IPipeContext context, Func<IPipeContext, object> contextFunc)
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
