using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Subscribe
{
	public class SubscriptionConfiguration : IConsumerConfiguration
	{
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; set; }
		public ExchangeDeclaration Exchange { get; set; }
		public QueueDeclaration Queue { get; set; }
		public string RoutingKey { get; set; }

		public SubscriptionConfiguration()
		{
			Exchange = new ExchangeDeclaration();
			Queue = new QueueDeclaration();
		}
	}
}
