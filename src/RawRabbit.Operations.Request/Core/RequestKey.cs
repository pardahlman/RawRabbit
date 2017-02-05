namespace RawRabbit.Operations.Request.Core
{
	public static class RequestKey
	{
		public const string IncommingMessageType = "IncommingMessageType";
		public const string CorrelationId = "CorrelationId";
		public const string SerializedResponse = "SerializedResponse";
		public const string OutgoingMessageType = "OutgoingMessageType";
		public const string ResponseMessage = "ResponseMessage";
		public const string RequestMessage = "RequestMessage";
		public const string Configuration = "RequestConfiguration";
		public const string PublicationAddress = "PublicationAddress";
		public const string ResponseQueueConfig = "ResponseQueueConfig";
	}
}
