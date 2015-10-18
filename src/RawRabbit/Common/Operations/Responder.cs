using System;
using System.Threading.Tasks;
using RawRabbit.Core.Configuration.Respond;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IResponder
	{
		Task
			RespondAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, TResponse> onMessage, ResponderConfiguration configuration = null)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}

	public class Responder : IResponder
	{
		public Task
			RespondAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, TResponse> onMessage, ResponderConfiguration configuration = null)
				where TRequest : MessageBase
				where TResponse : MessageBase
		{
			return Task.FromResult(true);
		}
	}
}
