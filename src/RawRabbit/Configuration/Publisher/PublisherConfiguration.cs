using System;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher
{
	public class PublisherConfiguration : BasicPublishConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public Action<BasicReturnEventArgs> MandatoryCallback { get; set; }
	}
}