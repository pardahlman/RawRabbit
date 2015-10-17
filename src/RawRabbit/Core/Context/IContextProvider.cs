using System.Threading.Tasks;

namespace RawRabbit.Core.Context
{
	public interface IContextProvider
	{
		string GetSessionId();
		Task<string> GetSessionIdAsync();
	}
}
