using System;
using RawRabbit.Core.Configuration.Exchange;

namespace RawRabbit.Core.Configuration.Respond
{
	public interface IResponderConfigurationBuilder
	{
		IResponderConfigurationBuilder WithReplyQueue(string replyTo);
		IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange);
	}
}
