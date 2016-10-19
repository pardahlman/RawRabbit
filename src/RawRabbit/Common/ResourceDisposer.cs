using System;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Common
{
	public interface IResourceDisposer :IDisposable
	{
	}

	public class ResourceDisposer : IResourceDisposer
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConnectionFactory _connectionFactory;

		public ResourceDisposer(IChannelFactory channelFactory, IConnectionFactory connectionFactory)
		{
			_channelFactory = channelFactory;
			_connectionFactory = connectionFactory;
		}

		public void Dispose()
		{
			_channelFactory.Dispose();
			(_connectionFactory as IDisposable)?.Dispose();
		}
	}
}
