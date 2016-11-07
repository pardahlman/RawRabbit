using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Respond;
using RawRabbit.Pipe;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfiguration : IConsumerConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }

		/* Response Queue Configuration*/
		public bool NoAck { get; set; }
		public ushort PrefetchCount => 1; // Only expect one response
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
