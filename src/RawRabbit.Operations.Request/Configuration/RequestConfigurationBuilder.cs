using System;
using RawRabbit.Configuration.Consume;
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

		public IRequestConfigurationBuilder PublishRequest(Action<IPublishConfigurationBuilder> publish)
		{
			var builder = new PublishConfigurationBuilder(Config.Request);
			publish(builder);
			Config.Request = builder.Config;
			return this;
		}

		public IRequestConfigurationBuilder ConsumeResponse(Action<IConsumeConfigurationBuilder> consume)
		{
			var builder = new ConsumeConfigurationBuilder(Config.Response);
			consume(builder);
			Config.Response = builder.Config;
			return this;
		}
	}
}
