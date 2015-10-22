using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Operation
{
	public abstract class ConfigurationBuilderBase
	{
		protected QueueConfigurationBuilder _replyQueue;
		protected ExchangeConfigurationBuilder _exchange;
		protected string RoutingKey;

		protected ConfigurationBuilderBase()
		{
			_replyQueue = new QueueConfigurationBuilder();
			_exchange = new ExchangeConfigurationBuilder();
		}

	}
}
