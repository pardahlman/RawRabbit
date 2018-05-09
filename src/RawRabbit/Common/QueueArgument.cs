namespace RawRabbit.Common
{
	public class QueueArgument
	{
		/// <summary>
		/// Indicates that the queue is a priority queue that honours the <br />
		/// priority property of a received message.
		/// </summary>
		public static readonly string MaxPriority = "x-max-priority";

		/// <summary>
		/// Sets what exchange that will be used as a dead letter exchange. <br/>
		/// More information: https://www.rabbitmq.com/dlx.html
		/// </summary>
		public static readonly string DeadLetterExchange = "x-dead-letter-exchange";

		/// <summary>
		/// Sets the Time To Live (TTL) in milliseconds for messages in the queue.
		/// </summary>
		public static readonly string MessageTtl = "x-message-ttl";

		/// <summary>
		/// Set QueueMode for a queue. Valid modes are "default" and "layz".<br />
		/// The messages for a lazy queue is held on disc and only loaded to
		/// RAM when requested by consumer.
		/// </summary>
		public static readonly string QueueMode = "x-queue-mode";

		/// <summary>
		/// Controls for how long a queue can be unused before it is automatically deleted.
		/// Unused means the queue has no consumers, the queue has not been redeclared, and
		/// basic.get has not been invoked for a duration of at least the expiration period
		/// </summary>
		public static readonly string Expires = "x-expires";
	}
}
