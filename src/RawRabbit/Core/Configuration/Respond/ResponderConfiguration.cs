using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Respond
{
	public class ResponderConfiguration
	{
		public string ReplyTo { get; set; }
		public QueueConfiguration Queue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public ushort PrefetchCount { get; set; }
	}
}
