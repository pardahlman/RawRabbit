using System.Collections.Generic;

namespace RawRabbit.Configuration.Exchange
{
	public class ExchangeDeclaration
	{
		public string ExchangeName { get; set; }
		public string ExchangeType { get; set; }
		public bool Durable { get; set; }
		public bool AutoDelete { get; set; }
		public IDictionary<string,object> Arguments { get; set; }
		public bool AssumeInitialized { get; set; }

		public ExchangeDeclaration()
		{
			Arguments = new Dictionary<string, object>();
		}

		public ExchangeDeclaration(GeneralExchangeConfiguration exchange) : this()
		{
			Durable = exchange.Durable;
			AutoDelete = exchange.AutoDelete;
			ExchangeType = exchange.Type.ToString().ToLower();
		}

		public static ExchangeDeclaration Default => new ExchangeDeclaration
		{
			ExchangeName = "",
			ExchangeType = RabbitMQ.Client.ExchangeType.Topic
		};

	}
}
