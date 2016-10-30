using System.Threading;
using RawRabbit.Context;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.MessageContext.Chaining.Dependencies
{
	public interface IMessageContextRepository
	{
		IMessageContext Get();
		void Set(IMessageContext context);
	}

	public class MessageContextRepository : IMessageContextRepository
	{

#if NETSTANDARD1_5
		private readonly AsyncLocal<IMessageContext> _msgContext;
#elif NET451
		private const string MessageContext = "RawRabbit:MessageContext";
#endif

		public MessageContextRepository()
		{
#if NETSTANDARD1_5
			_msgContext = new AsyncLocal<IMessageContext>();
#endif
		}
		public IMessageContext Get()
		{
#if NETSTANDARD1_5
			return _msgContext?.Value;
#elif NET451
			return CallContext.LogicalGetData(MessageContext) as IMessageContext;
#endif
		}

		public void Set(IMessageContext context)
		{
#if NETSTANDARD1_5
			_msgContext.Value = context;
#elif NET451
			CallContext.LogicalSetData(MessageContext, context);
#endif
		}
	}
}
