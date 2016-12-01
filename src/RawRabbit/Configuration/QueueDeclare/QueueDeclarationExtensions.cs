	using System;

namespace RawRabbit.Configuration.Queue
{
	public static class QueueDecclarationExtensions
	{
		private static readonly string _directQueueName = "amq.rabbitmq.reply-to";

		public static bool IsDirectReplyTo(this QueueDeclaration queue)
		{
			return string.Equals(queue.Name, _directQueueName, StringComparison.CurrentCultureIgnoreCase);
		}
	}
}
