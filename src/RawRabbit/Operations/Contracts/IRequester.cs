using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Request;

namespace RawRabbit.Operations.Contracts
{
	public interface IRequester
	{
		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config);
	}
}