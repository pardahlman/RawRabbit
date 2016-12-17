using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscribeInvokationMiddleware : HandlerInvokationMiddleware
	{
		public SubscribeInvokationMiddleware() : base(new HandlerInvokationOptions
		{
			HandlerArgsFunc = context => new []{ context.GetMessage()}
		})
		{ }
	}
}
