using System;
using System.Threading.Tasks;

namespace RawRabbit.Common
{
	public interface IShutdown
	{
		/// <summary>
		/// Shuts down the client and disposes all resources such as channels, connections and subscribers.
		/// </summary>
		/// <param name="graceful">The amout of time to wait for Consumer methods to process message before shutting down its channel</param>
		/// <returns></returns>
		Task ShutdownAsync(TimeSpan? graceful = null);
	}
}
