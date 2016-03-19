using System.Threading.Tasks;

namespace RawRabbit.Common
{
	public interface IShutdown
	{
		Task ShutdownAsync();
	}
}
