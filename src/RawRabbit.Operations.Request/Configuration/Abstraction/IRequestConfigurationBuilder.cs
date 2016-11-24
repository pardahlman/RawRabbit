using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Publisher;

namespace RawRabbit.Operations.Request.Configuration.Abstraction
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder PublishRequest(Action<IPublisherConfigurationBuilder> publish);
		IRequestConfigurationBuilder ConsumeResponse(Action<IConsumeConfigurationBuilder> consume);
	}
}