using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Subscribe
{
	public class SubscriptionConfiguration
	{
		public ExchangeConfiguration ExchangeConfiguration { get; set; }
		public QueueConfiguration QueueConfiguration { get; set; }

		public static SubscriptionConfiguration Default => new SubscriptionConfiguration
		{
			QueueConfiguration = QueueConfiguration.Default,
			ExchangeConfiguration = ExchangeConfiguration.Default
		};
	}
}
