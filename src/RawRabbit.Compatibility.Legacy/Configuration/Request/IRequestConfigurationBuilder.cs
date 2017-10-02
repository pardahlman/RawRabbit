using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Request
{
	public interface IRequestConfigurationBuilder
	{
		IRequestConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
		IRequestConfigurationBuilder WithRoutingKey(string routingKey);
		IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo);
		[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
		IRequestConfigurationBuilder WithNoAck(bool noAck);
		IRequestConfigurationBuilder WithAutoAck(bool autoAck);
	}
}