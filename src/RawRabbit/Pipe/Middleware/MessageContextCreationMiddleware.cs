using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Context.Provider;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageContextCreationMiddleware<TMessageContext> : Middleware where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public MessageContextCreationMiddleware(IMessageContextProvider<TMessageContext> contextProvider)
		{
			_contextProvider = contextProvider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			if (context.Properties.ContainsKey(PipeKey.MessageContext))
			{
				return Next.InvokeAsync(context);
			}

			var globalMsgId = context.GetGlobalMessageId();
			var msgContext = _contextProvider.CreateMessageContext(globalMsgId);
			context.Properties.Add(PipeKey.MessageContext, msgContext);
			return Next.InvokeAsync(context);
		}
	}
}
