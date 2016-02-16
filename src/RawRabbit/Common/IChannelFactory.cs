using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Common
{
	public interface IChannelFactory : IDisposable
	{
		/// <summary>
		/// Retrieves a channel that is disposed by the channel factory
		/// </summary>
		/// <returns>A new or existing instance of an IModel</returns>
		IModel GetChannel();
		/// <summary>
		/// Creates a new istance of a channal that the caller is responsible
		/// in closing and disposing.
		/// </summary>
		/// <returns>A new instance of an IModel</returns>
		IModel CreateChannel(IConnection connection = null);

		Task<IModel> GetChannelAsync();
		Task<IModel> CreateChannelAsync(IConnection connection = null);
	}
}
