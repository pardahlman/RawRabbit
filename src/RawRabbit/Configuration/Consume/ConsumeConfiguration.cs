using System.Collections.Generic;

namespace RawRabbit.Configuration.Consume
{
	public class ConsumeConfiguration
	{
		public string QueueName { get; set; }
		public string ExchangeName { get; set; }
		public bool NoAck { get; set; }
		public string ConsumerTag { get; set; }
		public string RoutingKey { get; set; }
		public bool NoLocal { get; set; }
		public ushort PrefetchCount { get; set; }
		public bool Exclusive { get; set; }
		public Dictionary<string, object> Arguments { get; set; }
	}
}