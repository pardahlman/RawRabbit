using System.Threading.Tasks;
using RawRabbit.Context.Enhancer;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageContextEnhanceMiddleware : Middleware
	{
		private readonly IContextEnhancer _contextEnhancer;

		public MessageContextEnhanceMiddleware(IContextEnhancer contextEnhancer)
		{
			_contextEnhancer = contextEnhancer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();
			var consumer = context.GetConsumer();
			var args = context.GetDeliveryEventArgs();

			_contextEnhancer.WireUpContextFeatures(msgContext, consumer, args);

			return Next.InvokeAsync(context);
		}
	}
}
