namespace RawRabbit.Common
{
	public class QueueArgument
	{
		public static readonly string MaxPriority = "x-max-priority";
		public static readonly string DeadLetterExchange = "x-dead-letter-exchange";
		public static readonly string MessageTtl = "x-message-ttl";

		/// <summary>
		/// Set QueueMode for a queue. Valid modes are "default" and "layz".<br />
		/// The messages for a lazy queue is held on disc and only loaded to
		/// RAM when requested by consumer.
		/// </summary>
		public static readonly string QueueMode = "x-queue-mode";
	}
}
