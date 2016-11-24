using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public interface IConsumerConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IConsumerConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange);

		/// <summary>
		/// Specify the topology features of the Queue to consume from.
		/// Queue will be declared.
		/// /// </summary>
		/// <param name="queue"></param>
		/// <returns></returns>
		IConsumerConfigurationBuilder FromDeclaredQueue(Action<IQueueConfigurationBuilder> queue);
		IConsumerConfigurationBuilder WithNoAck(bool noAck = true);
		IConsumerConfigurationBuilder WithConsumerTag(string tag);
		IConsumerConfigurationBuilder WithRoutingKey(string routingKey);
		IConsumerConfigurationBuilder WithNoLocal(bool noLocal = true);
		IConsumerConfigurationBuilder WithPrefetchCount(ushort prefetch);
		IConsumerConfigurationBuilder WithExclusive(bool exclusive = true);
		IConsumerConfigurationBuilder WithArgument(string key, object value);
	}
}