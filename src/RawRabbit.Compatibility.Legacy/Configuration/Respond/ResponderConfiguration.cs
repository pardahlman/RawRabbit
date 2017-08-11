using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Respond
{
	public class ResponderConfiguration : IConsumerConfiguration
	{
		public bool AutoAck { get; set; }
		public ushort PrefetchCount { get; set; }
		public ExchangeConfiguration Exchange { get; set; }
		public QueueConfiguration Queue { get; set; }
		public string RoutingKey { get; set; }

		public ResponderConfiguration()
		{
			Exchange = new ExchangeConfiguration();
			Queue = new QueueConfiguration();
			AutoAck = true;
		}
	}

	public interface IConsumerConfiguration : IOperationConfiguration
	{
		bool AutoAck { get; }
		ushort PrefetchCount { get; }
	}

	public interface IOperationConfiguration
	{
		ExchangeConfiguration Exchange { get; }
		QueueConfiguration Queue { get; }
		string RoutingKey { get; }
	}
}