using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfigurationFactory : IRequestConfigurationFactory
	{
		private readonly IPublishConfigurationFactory _publish;
		private readonly IConsumeConfigurationFactory _consume;

		public RequestConfigurationFactory(IPublishConfigurationFactory publish, IConsumeConfigurationFactory consume)
		{
			_publish = publish;
			_consume = consume;
		}

		public RequestConfiguration Create<TRequest, TResponse>()
		{
			return Create(typeof(TRequest), typeof(TResponse));
		}

		public RequestConfiguration Create(Type requestType, Type responseType)
		{
			var cfg = new RequestConfiguration
			{
				Request = _publish.Create(requestType),
				Response = _consume.Create(responseType)
			};
			cfg.ToDirectRpc();
			return cfg;
		}

		public RequestConfiguration Create(string requestExchange, string requestRoutingKey, string responseQueue, string responseExchange, string responseRoutingKey)
		{
			var cfg = new RequestConfiguration
			{
				Request = _publish.Create(requestExchange, requestRoutingKey),
				Response = _consume.Create(responseQueue, responseExchange, responseRoutingKey)
			};
			cfg.ToDirectRpc();
			return cfg;
		}
	}
}
