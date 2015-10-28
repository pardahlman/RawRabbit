using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Respond;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfiguration : IConsumerConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }

		/* Response Queue Configuration*/
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; }
		public QueueConfiguration Queue => ReplyQueue;
		public QueueConfiguration ReplyQueue { get; set; }
		public string ReplyQueueRoutingKey { get; set; }

		public RequestConfiguration()
		{
			Exchange = new ExchangeConfiguration();
			ReplyQueue = new QueueConfiguration();
			NoAck = true;
		}
	}
}
