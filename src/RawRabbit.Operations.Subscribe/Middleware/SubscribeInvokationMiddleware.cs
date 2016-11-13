using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscribeInvokationMiddleware : MessageHandlerInvokationMiddleware
	{
		public SubscribeInvokationMiddleware() : base(new MessageHandlerInvokationOptions
		{
			HandlerArgsFunc = context => new []{ context.GetMessage()}
		})
		{ }
	}
}
