using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Legacy.Request;

namespace RawRabbit.Operations.Abstraction
{
	public interface IRequester
	{
		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config);
	}
}