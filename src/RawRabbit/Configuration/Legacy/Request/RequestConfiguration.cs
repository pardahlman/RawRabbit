using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Request
{
	public class RequestConfiguration : IConsumerConfiguration
	{
		public ExchangeDeclaration Exchange { get; set; }
		public string RoutingKey { get; set; }

		/* Response Queue Configuration*/
		public bool NoAck { get; set; }
		public ushort PrefetchCount => 1; // Only expect one response
		public QueueDeclaration Queue => ReplyQueue;
		public QueueDeclaration ReplyQueue { get; set; }
		public string ReplyQueueRoutingKey { get; set; }

		public RequestConfiguration()
		{
			Exchange = new ExchangeDeclaration();
			ReplyQueue = new QueueDeclaration();
			NoAck = true;
		}
	}
}
