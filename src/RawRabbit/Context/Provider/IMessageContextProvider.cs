using System;
using System.Threading.Tasks;

namespace RawRabbit.Context.Provider
{
	public interface IMessageContextProvider<TMessageContext> where TMessageContext : IMessageContext
	{
		Task<object> GetMessageContextAsync(Guid globalMessageId);
		Task<TMessageContext> ExtractContextAsync(object o);
		TMessageContext ExtractContext(object o);
		object GetMessageContext(ref Guid globalMessageId);
	}
}
