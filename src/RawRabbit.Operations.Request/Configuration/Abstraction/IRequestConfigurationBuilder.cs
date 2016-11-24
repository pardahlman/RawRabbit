using System;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Operations.Request.Configuration.Abstraction
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder PublishRequest(Action<IPublisherConfigurationBuilder> publish);
		IRequestConfigurationBuilder ConsumeResponse(Action<IConsumeConfigurationBuilder> consume);
	}
}