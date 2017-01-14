using System;

namespace RawRabbit.Compatibility.Legacy.Configuration.Queue
{
	public static class QueueConfigurationExtensions
	{
		private static readonly string _directQueueName = "amq.rabbitmq.reply-to";

		public static bool IsDirectReplyTo(this QueueConfiguration queue)
		{
			return string.Equals(queue.QueueName, _directQueueName, StringComparison.CurrentCultureIgnoreCase);
		}
	}
}
