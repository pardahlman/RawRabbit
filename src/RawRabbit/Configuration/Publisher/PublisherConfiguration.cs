using System;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfiguration : BasicPublishConfiguration
	{
		public ExchangeDeclaration Exchange { get; set; }
		public EventHandler<BasicReturnEventArgs> ReturnCallback { get; set; }
	}
}
