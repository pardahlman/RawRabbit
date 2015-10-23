using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public QueueConfiguration ReplyQueue { get; set; }
		public string ReplyQueueRoutingKey { get; set; }
		public string RoutingKey { get; set; }

		public RequestConfiguration()
		{
			Exchange = new ExchangeConfiguration();
			ReplyQueue = new QueueConfiguration();
		}
	}
}
