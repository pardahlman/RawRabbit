using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfiguration
	{
		public ConsumerConfiguration Response { get; set; }
		public PublisherConfiguration Request { get; set; }
	}

	public static class RequestConfigurationExtensions
	{
		private const string DirectReplyTo = "amq.rabbitmq.reply-to";
		private const string DefaultExchange = "";

		public static RequestConfiguration ToDirectRpc(this RequestConfiguration config)
		{
			config.Response.Queue.Name = DirectReplyTo;
			config.Response.Consume.QueueName = DirectReplyTo;
			config.Response.Consume.RoutingKey = DirectReplyTo;
			config.Response.Exchange.Name = DefaultExchange;
			config.Response.Consume.ExchangeName = DefaultExchange;
			config.Response.Consume.NoAck = true;
			return config;
		} 
	}
}