using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Operation
{
	public class ConfigurationBase
	{
		public ExchangeConfiguration Exchange { get; set; }
		public QueueConfiguration Queue { get; set; }
		public string RoutingKey { get; set; }

		public ConfigurationBase()
		{
			Exchange = new ExchangeConfiguration();
			Queue = new QueueConfiguration();
		}
	}
}
