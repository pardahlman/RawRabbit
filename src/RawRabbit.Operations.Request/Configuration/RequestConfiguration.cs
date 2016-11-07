using RawRabbit.Configuration.Consume;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfiguration
	{
		public ConsumeConfiguration Response { get; set; }
		public PublishConfiguration Request { get; set; }
	}

	public static class RequestConfigurationExtensions
	{
		private const string DirectReplyTo = "amq.rabbitmq.reply-to";
		private const string DefaultExchange = "";

		public static RequestConfiguration ToDirectRpc(this RequestConfiguration config)
		{
			config.Response.Queue.QueueName = DirectReplyTo;
			config.Response.RoutingKey = DirectReplyTo;
			config.Response.Exchange.ExchangeName = DefaultExchange;
			config.Response.NoAck = true;
			return config;
		} 
	}
}