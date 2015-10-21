using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Request
{
	public class RequestConfiguration
	{
		public QueueConfiguration ReplyQueue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }

		public RequestConfiguration()
		{
			ReplyQueue = new QueueConfiguration();
			Exchange = new ExchangeConfiguration();
		}
	}
}
