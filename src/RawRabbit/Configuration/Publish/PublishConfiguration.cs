using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfiguration
	{
		public QueueConfiguration Queue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }
	}
}
