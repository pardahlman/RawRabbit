using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext
{
	public static class PipeContextExtensions
	{
		public static IPipeContext UseMessageContext(this IPipeContext context, object msgContext)
		{
			context.Properties.TryAdd(PipeKey.MessageContext, msgContext);
			return context;
		}
	}
}
