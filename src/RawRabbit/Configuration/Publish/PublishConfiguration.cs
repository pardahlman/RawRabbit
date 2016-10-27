using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publish
{
	public class PublishConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public string RoutingKey { get; set; }
		public Action<IBasicProperties> PropertyModifier { get; set; }
		public EventHandler<BasicReturnEventArgs> ReturnCallback { get; set; }
	}
}
