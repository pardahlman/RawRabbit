namespace RawRabbit.Configuration.Consume
{
	public interface IConsumeConfigurationBuilder {
		IConsumeConfigurationBuilder OnExchange(string exchange);
		IConsumeConfigurationBuilder FromQueue(string queue);
		IConsumeConfigurationBuilder WithNoAck(bool noAck = true);
		IConsumeConfigurationBuilder WithConsumerTag(string tag);
		IConsumeConfigurationBuilder WithRoutingKey(string routingKey);
		IConsumeConfigurationBuilder WithNoLocal(bool noLocal = true);
		IConsumeConfigurationBuilder WithPrefetchCount(ushort prefetch);
		IConsumeConfigurationBuilder WithExclusive(bool exclusive = true);
		IConsumeConfigurationBuilder WithArgument(string key, object value);
	}
}