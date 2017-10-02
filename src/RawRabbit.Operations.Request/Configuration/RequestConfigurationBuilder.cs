using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		public RequestConfiguration Config { get; }

		public RequestConfigurationBuilder(RequestConfiguration initial)
		{
			Config = initial;
		}

		public IRequestConfigurationBuilder PublishRequest(Action<IPublisherConfigurationBuilder> publish)
		{
			var builder = new PublisherConfigurationBuilder(Config.Request);
			publish(builder);
			Config.Request = builder.Config;
			return this;
		}

		public IRequestConfigurationBuilder ConsumeResponse(Action<IConsumerConfigurationBuilder> consume)
		{
			var builder = new ConsumerConfigurationBuilder(Config.Response);
			consume(builder);
			Config.Response = builder.Config;
			return this;
		}
	}
}
