using System;
using RabbitMQ.Client;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }
		public Action<IBasicProperties> PropertyModifier { get; set; }
	}
}
