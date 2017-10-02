using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;
using RawRabbit.Compatibility.Legacy.Configuration.Respond;

namespace RawRabbit.Compatibility.Legacy.Configuration.Request
{
	public class RequestConfiguration : IConsumerConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }

		/* Response Queue Configuration*/
		public bool AutoAck { get; set; }
		public ushort PrefetchCount => 1; // Only expect one response
		public QueueConfiguration Queue => ReplyQueue;
		public QueueConfiguration ReplyQueue { get; set; }
		public string ReplyQueueRoutingKey { get; set; }

		public RequestConfiguration()
		{
			Exchange = new ExchangeConfiguration();
			ReplyQueue = new QueueConfiguration();
			AutoAck = true;
		}
	}
}
