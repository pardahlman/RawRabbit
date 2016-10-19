using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Context;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Pipe.Middleware
{
	public class MessageChainingMiddleware : Middleware
	{
#if NETSTANDARD1_5
		private readonly AsyncLocal<IMessageContext> _msgContext;
#elif NET451
		private const string MessageContext = "RawRabbit:MessageContext";
#endif

		public MessageChainingMiddleware()
		{
#if NETSTANDARD1_5
			_msgContext = new AsyncLocal<IMessageContext>();
#endif
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();

			if (msgContext != null)
			{
#if NETSTANDARD1_5
				_msgContext.Value = msgContext;
#elif NET451
				CallContext.LogicalSetData(MessageContext, msgContext);
#endif
			}
			else
			{
#if NETSTANDARD1_5
				msgContext = _msgContext?.Value;
#elif NET451
				msgContext = CallContext.LogicalGetData(MessageContext) as IMessageContext;
#endif
				if (msgContext != null)
				{
					context.Properties.Add(PipeKey.MessageContext, msgContext);
				}
			}

			return Next.InvokeAsync(context);
		}
	}
}