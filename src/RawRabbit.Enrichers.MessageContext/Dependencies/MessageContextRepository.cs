using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.MessageContext.Dependencies
{
	public interface IMessageContextRepository
	{
		object Get();
		void Set(object context);
	}

	public class MessageContextRepository : IMessageContextRepository
	{

#if NETSTANDARD1_5 || NETSTANDARD2_0
		private readonly AsyncLocal<object> _msgContext;
#elif NET451
		private const string MessageContext = "RawRabbit:MessageContext";
#endif

		public MessageContextRepository()
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			_msgContext = new AsyncLocal<object>();
#endif
		}
		public object Get()
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			return _msgContext?.Value;
#elif NET451
			return CallContext.LogicalGetData(MessageContext) as object;
#endif
		}

		public void Set(object context)
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			_msgContext.Value = context;
#elif NET451
			CallContext.LogicalSetData(MessageContext, context);
#endif
		}
	}
}
