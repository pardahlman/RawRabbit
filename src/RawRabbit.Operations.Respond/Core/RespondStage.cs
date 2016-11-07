namespace RawRabbit.Operations.Respond.Core
{
	public static class RespondStage
	{
		public const string Initiated = "Initiated";
		public const string ConfigurationCreated = "ConfigurationCreated";
		public const string QueueDeclared = "QueueDeclared";
		public const string ExchangeDeclared = "ExchangeDeclared";
		public const string QueueBound = "ExchangeDeclared";
		public const string ConsumerChannelCreated = "ConsumerChannelCreated";
		public const string ConsumerCreated = "ConsumerCreated";
		public const string MessageRecieved = "MessageRecieved";
		public const string MessageDeserialized = "MessageDeserialized";
		public const string HandlerInvoked = "HandlerInvoked";
		public const string BasicPropertiesCreated = "BasicPropertiesCreated";
		public const string ResponseSerialized = "ResponseSerialized";
		public const string ReplyToExtracted = "ReplyToExtracted";
		public const string RespondChannelCreated = "RespondChannelCreated";
		public const string ResponsePublished = "ResponsePublished";
	}
}
