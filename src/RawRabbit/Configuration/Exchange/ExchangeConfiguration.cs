using System.Collections.Generic;

namespace RawRabbit.Configuration.Exchange
{
	public class ExchangeConfiguration
	{
		public string ExchangeName { get; set; }
		public string ExchangeType { get; set; }
		public bool Durable { get; set; }
		public bool AutoDelete { get; set; }
		public Dictionary<string,string> Arguments { get; set; }
		public bool AssumeInitialized { get; set; }

		public ExchangeConfiguration()
		{
			Arguments = new Dictionary<string, string>();
		}

		public static ExchangeConfiguration Default => new ExchangeConfiguration
		{
			ExchangeName = "",
			ExchangeType = RabbitMQ.Client.ExchangeType.Direct
		};

	}
}
