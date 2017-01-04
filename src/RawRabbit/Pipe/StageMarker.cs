namespace RawRabbit.Pipe
{
	public static class StageMarker
	{
		public const string Initialized = "Initialized";
		public const string PublishConfigured = "PublishConfigured";
		public const string ConsumeConfigured = "ConsumeConfigured";
		public const string BasicPropertiesCreated = "BasicPropertiesCreated";
		public const string MessageRecieved = "MessageRecieved";
		public const string MessageDeserialized = "MessageDeserialized";
		public const string HandlerInvoked = "HandlerInvoked";
	}
}
