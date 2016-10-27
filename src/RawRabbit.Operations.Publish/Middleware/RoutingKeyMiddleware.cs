using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class RoutingKeyMiddleware : Pipe.Middleware.Middleware
	{
		private readonly INamingConventions _conventions;

		public RoutingKeyMiddleware(INamingConventions conventions)
		{
			_conventions = conventions;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgType = context.GetMessageType();
			var routingKey = _conventions.RoutingKeyConvention(msgType);
			context.Properties.Add(PipeKey.RoutingKey, routingKey);
			return Next.InvokeAsync(context);
		}
	}
}
