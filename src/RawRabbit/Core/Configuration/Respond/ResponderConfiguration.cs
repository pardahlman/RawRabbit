using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Respond
{
	public class ResponderConfiguration
	{
		public QueueConfiguration Queue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public ushort PrefetchCount { get; set; }
		public string RoutingKey { get; set; }
	}
}
