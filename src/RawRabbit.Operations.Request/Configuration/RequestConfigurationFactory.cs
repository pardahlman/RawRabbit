using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfigurationFactory : IRequestConfigurationFactory
	{
		private readonly IPublisherConfigurationFactory _publisher;
		private readonly IConsumerConfigurationFactory _consumer;

		public RequestConfigurationFactory(IPublisherConfigurationFactory publisher, IConsumerConfigurationFactory consumer)
		{
			_publisher = publisher;
			_consumer = consumer;
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
				Response = _consumer.Create(responseType)
			};
			cfg.ToDirectRpc();
			return cfg;
		}

		public RequestConfiguration Create(string requestExchange, string requestRoutingKey, string responseQueue, string responseExchange, string responseRoutingKey)
		{
			var cfg = new RequestConfiguration
			{
				Request = _publisher.Create(requestExchange, requestRoutingKey),
				Response = _consumer.Create(responseQueue, responseExchange, responseRoutingKey)
			};
			cfg.ToDirectRpc();
			return cfg;
		}
	}
}
