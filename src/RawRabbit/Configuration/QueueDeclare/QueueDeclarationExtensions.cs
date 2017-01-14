	using System;

namespace RawRabbit.Configuration.Queue
{
	public static class QueueDecclarationExtensions
	{
		internal static readonly string DirectQueueName = "amq.rabbitmq.reply-to";

		public static bool IsDirectReplyTo(this QueueDeclaration queue)
		{
			return string.Equals(queue.Name, DirectQueueName, StringComparison.CurrentCultureIgnoreCase);
		}
	}
}
