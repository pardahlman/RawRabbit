using System;

namespace RawRabbit.Configuration.BasicPublish
{
	public interface IBasicPublishConfigurationFactory
	{
		BasicPublishConfiguration Create();
		BasicPublishConfiguration Create(Type type);
		BasicPublishConfiguration Create(object message);
	}
}