using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Subscribe
{
	public class SubscriptionConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public QueueConfiguration Queue { get; set; }
		public string RoutingKey { get; set; }
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; set; }

		public static SubscriptionConfiguration Default => new SubscriptionConfiguration
		{
			Queue = QueueConfiguration.Default,
			Exchange = ExchangeConfiguration.Default
		};
	}
}
