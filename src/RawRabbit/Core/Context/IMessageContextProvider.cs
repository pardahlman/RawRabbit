using System.Threading.Tasks;
using RawRabbit.Core.Message;

namespace RawRabbit.Core.Context
{
	public interface IMessageContextProvider<TMessageContext> where TMessageContext : MessageContext
	{
		string ContextHeaderName { get; }
		Task<object> GetMessageContextAsync();
		Task<TMessageContext> ExtractContextAsync(object o);
	}
}
