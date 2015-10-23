using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Respond
{
	public class ResponderConfiguration
	{
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public QueueConfiguration Queue { get; set; }
		public string RoutingKey { get; set; }

		public ResponderConfiguration()
		{
			Exchange = new ExchangeConfiguration();
			Queue = new QueueConfiguration();
		}
	}
}
