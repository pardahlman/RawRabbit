using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfigurationFactory : IRequestConfigurationFactory
	{
		private readonly IPublisherConfigurationFactory _publisher;
		private readonly IConsumeConfigurationFactory _consume;

		public RequestConfigurationFactory(IPublisherConfigurationFactory publisher, IConsumeConfigurationFactory consume)
		{
			_publisher = publisher;
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
				Request = _publisher.Create(requestType),
				Response = _consume.Create(responseType)
			};
			cfg.ToDirectRpc();
			return cfg;
		}

		public RequestConfiguration Create(string requestExchange, string requestRoutingKey, string responseQueue, string responseExchange, string responseRoutingKey)
		{
			var cfg = new RequestConfiguration
			{
				Request = _publisher.Create(requestExchange, requestRoutingKey),
				Response = _consume.Create(responseQueue, responseExchange, responseRoutingKey)
			};
			cfg.ToDirectRpc();
			return cfg;
		}
	}
}
