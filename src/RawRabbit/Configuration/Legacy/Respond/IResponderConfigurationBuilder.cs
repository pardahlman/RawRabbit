using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Respond
{
	public interface IResponderConfigurationBuilder
	{
		IResponderConfigurationBuilder WithPrefetchCount(ushort count);
		IResponderConfigurationBuilder WithExchange(Action<IExchangeDeclarationBuilder> exchange);
		IResponderConfigurationBuilder WithQueue(Action<IQueueDeclarationBuilder> queue);
		IResponderConfigurationBuilder WithRoutingKey(string routingKey);
		IResponderConfigurationBuilder WithNoAck(bool noAck);
	}
}
