using System.Collections.Generic;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfiguration
	{
		public QueueDeclaration Queue { get; set; }
		public ExchangeDeclaration Exchange { get; set; }
		public ConsumeConfiguration Consume { get; set; }
	}
}