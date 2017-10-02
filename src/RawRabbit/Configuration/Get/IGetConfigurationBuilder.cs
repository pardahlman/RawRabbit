using System;

namespace RawRabbit.Configuration.Get
{
	public interface IGetConfigurationBuilder
	{
		IGetConfigurationBuilder FromQueue(string queueName);
		[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
		IGetConfigurationBuilder WithNoAck(bool noAck = true);
		IGetConfigurationBuilder WithAutoAck(bool autoAck = true);
	}
}
