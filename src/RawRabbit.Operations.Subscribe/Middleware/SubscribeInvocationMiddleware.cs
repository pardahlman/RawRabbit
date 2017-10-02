using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscribeInvocationMiddleware : HandlerInvocationMiddleware
	{
		public SubscribeInvocationMiddleware() : base(new HandlerInvocationOptions
		{
			HandlerArgsFunc = context => new []{ context.GetMessage()}
		})
		{ }
	}
}
