namespace RawRabbit.Core.Configuration.Queue
{
	public class QueueConfiguration
	{
		public string QueueName { get; set; }
		public string ExchangeName { get; set; }
		public string RoutingKey { get; set; }

		public static QueueConfiguration Default => new QueueConfiguration
		{
			ExchangeName = ""
		};
	}
}
