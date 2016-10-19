using System;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class GlobalMessageIdMiddleware : Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();
			var globalMsgId = msgContext?.GlobalRequestId ?? Guid.NewGuid();
			context.Properties.Add(PipeKey.GlobalMessageId, globalMsgId);
			return Next.InvokeAsync(context);
		}
	}
}
