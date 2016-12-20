using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Channel.Abstraction
{
	public interface IChannelFactory : IDisposable
	{
		/// <summary>
		/// Retrieves a channel that is disposed by the channel factory
		/// </summary>
		/// <returns>A new or existing instance of an IModel</returns>
		Task<IModel> GetChannelAsync(CancellationToken token = default(CancellationToken));
		/// <summary>
		/// Creates a new istance of a channal that the caller is responsible
		/// in closing and disposing.
		/// </summary>
		/// <returns>A new instance of an IModel</returns>
		Task<IModel> CreateChannelAsync(CancellationToken token = default(CancellationToken));
	}
}
