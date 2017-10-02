using System;

namespace RawRabbit.Operations.Request.Configuration.Abstraction
{
	public interface IRequestConfigurationFactory
	{
		RequestConfiguration Create<TRequest, TResponse>();
		RequestConfiguration Create(Type requestType, Type responseType);
		RequestConfiguration Create(string requestExchange, string requestRoutingKey, string responseQueue, string responseExchange, string responseRoutingKey);
	}
}