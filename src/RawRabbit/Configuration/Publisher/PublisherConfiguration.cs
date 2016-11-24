using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public Action<BasicReturnEventArgs> MandatoryCallback { get; set; }
		public Action<IBasicProperties> PropertyModifier { get; set; }
		public string RoutingKey { get; set; }
	}
}