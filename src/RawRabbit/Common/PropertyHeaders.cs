namespace RawRabbit.Common
{
	public class PropertyHeaders
	{
		public static readonly string Sent = "sent";
		public static readonly string ApproximateRetry = "approx_retry";
		public static readonly string Death = "x-death";
		public static readonly string EstimatedRetry = "approx_retry";
		public static readonly string DeadLetterExchange = "x-dead-letter-exchange";
		public static readonly string MessageTtl = "x-message-ttl";
	}
}
