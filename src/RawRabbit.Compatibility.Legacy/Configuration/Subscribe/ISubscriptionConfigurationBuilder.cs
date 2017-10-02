using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Subscribe
{
	public interface ISubscriptionConfigurationBuilder
	{
		ISubscriptionConfigurationBuilder WithRoutingKey(string routingKey);
		ISubscriptionConfigurationBuilder WithPrefetchCount(ushort prefetchCount);
		[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
		ISubscriptionConfigurationBuilder WithNoAck(bool noAck = true);
		ISubscriptionConfigurationBuilder WithAutoAck(bool autoAck = true);
		ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue);

		/// <summary>
		/// The unique identifier for the subscriber. Note that the subscriber id must be
		/// unique for each routing key in order to get true topic behaviour on exchanges
		/// that supports that.
		/// </summary>
		/// <param name="subscriberId">The unique indeiindetifier for the subscriber.</param>
		/// <returns></returns>
		ISubscriptionConfigurationBuilder WithSubscriberId(string subscriberId);
	}
}
