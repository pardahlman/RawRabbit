using System;
using System.Threading.Tasks;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Request;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IRequester
	{
		Task RequestAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, RequestConfiguration config)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}

	public class Requester : OperatorBase, IRequester
	{
		private readonly IMessageSerializer _serializer;

		public Requester(IChannelFactory channelFactory, IMessageSerializer serializer) : base(channelFactory)
		{
			_serializer = serializer;
		}

		public Task RequestAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, RequestConfiguration config)
			where TRequest : MessageBase
			where TResponse : MessageBase
		{
			throw new NotImplementedException();
		}
	}
}