using System;
using RabbitMQ.Client.Events;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe
{
	public static class PipeContextExtensions
	{
		private const string PipebasedContextFunc = "PipebasedContextFunc";

		public static IPipeContext UseMessageContext(this IPipeContext context, Func<IPipeContext, object> contextFunc)
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
