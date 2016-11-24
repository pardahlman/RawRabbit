using System.Collections.Generic;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfiguration
	{
		public QueueConfiguration Queue { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public bool NoAck { get; set; }
		public string ConsumerTag { get; set; }
		public string RoutingKey { get; set; }
		public bool NoLocal { get; set; }
		public ushort PrefetchCount { get; set; }
		public bool Exclusive { get; set; }
		public Dictionary<string, object> Arguments { get; set; }
	}
}