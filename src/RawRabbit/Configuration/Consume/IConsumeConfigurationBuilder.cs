using System;

namespace RawRabbit.Configuration.Consume
{
	public interface IConsumeConfigurationBuilder {
		IConsumeConfigurationBuilder OnExchange(string exchange);
		IConsumeConfigurationBuilder FromQueue(string queue);
		[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
		IConsumeConfigurationBuilder WithNoAck(bool noAck = true);
		IConsumeConfigurationBuilder WithAutoAck(bool autoAck = true);
		IConsumeConfigurationBuilder WithConsumerTag(string tag);
		IConsumeConfigurationBuilder WithRoutingKey(string routingKey);
		IConsumeConfigurationBuilder WithNoLocal(bool noLocal = true);
		IConsumeConfigurationBuilder WithPrefetchCount(ushort prefetch);
		IConsumeConfigurationBuilder WithExclusive(bool exclusive = true);
		IConsumeConfigurationBuilder WithArgument(string key, object value);
	}
}