using System;
using System.Threading.Tasks;

namespace RawRabbit.Context
{
	public interface IMessageContextProvider<TMessageContext> where TMessageContext : MessageContext
	{
		string ContextHeaderName { get; }
		Task<object> GetMessageContextAsync(Guid globalMessageId);
		Task<TMessageContext> ExtractContextAsync(object o);
	}
}
