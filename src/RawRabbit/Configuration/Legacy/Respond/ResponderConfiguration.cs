using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Legacy.Respond
{
	public class ResponderConfiguration : IConsumerConfiguration
	{
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; set; }
		public ExchangeDeclaration Exchange { get; set; }
		public QueueDeclaration Queue { get; set; }
		public string RoutingKey { get; set; }

		public ResponderConfiguration()
		{
			Exchange = new ExchangeDeclaration();
			Queue = new QueueDeclaration();
			NoAck = true;
		}
	}

	public interface IConsumerConfiguration : IOperationConfiguration
	{
		bool NoAck { get; }
		ushort PrefetchCount { get; }
	}

	public interface IOperationConfiguration
	{
		ExchangeDeclaration Exchange { get; }
		QueueDeclaration Queue { get; }
		string RoutingKey { get; }
	}
}