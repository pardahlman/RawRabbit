using RabbitMQ.Client;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfiguration
	{
		public string ExchangeName { get; set; }
		public string RoutingKey { get; set; }
		public bool Mandatory { get; set; }
		public IBasicProperties BasicProperties { get; set; }
		public byte[] Body { get; set; }
	}
}