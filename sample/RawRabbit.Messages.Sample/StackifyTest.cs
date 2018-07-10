using RawRabbit.Configuration.Exchange;
using RawRabbit.Enrichers.Attributes;

namespace RawRabbit.Messages.Sample
{
	[Exchange(Type = ExchangeType.Topic, Name = "custom.stackify.exchange")]
	[Queue(Name = "custom.stackify.queue", Durable = false)]
	[Routing(RoutingKey = "custom.stackify.key")]
	public class StackifyTest
	{
		public string Value { get; set; }
	}
}
