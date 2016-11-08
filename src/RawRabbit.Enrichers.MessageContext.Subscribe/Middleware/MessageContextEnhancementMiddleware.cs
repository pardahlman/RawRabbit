using System.Threading.Tasks;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context.Enhancer;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
	public class MessageContextEnhancementMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IContextEnhancer _contextEnhancer;

		public MessageContextEnhancementMiddleware(IContextEnhancer contextEnhancer)
		{
			_contextEnhancer = contextEnhancer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();
			var consumer = context.Get<IRawConsumer>(PipeKey.Consumer);
			var args = context.GetDeliveryEventArgs();
			if (msgContext == null || consumer == null || args == null)
			{
				return Next.InvokeAsync(context);
			}
			_contextEnhancer.WireUpContextFeatures(msgContext, consumer, args);
			return Next.InvokeAsync(context);
		}
	}
}
