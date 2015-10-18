using RawRabbit.Core.Configuration.Queue;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Conventions
{
	public interface IQueueConventions
	{
		QueueConfiguration CreateQueueConfiguration<T>() where T: MessageBase;
	}

	public class QueueConventions : IQueueConventions
	{
		public QueueConfiguration CreateQueueConfiguration<T>() where T: MessageBase
		{
			return new QueueConfiguration
			{
				QueueName = typeof (T).Name.ToLower(),
				AutoDelete = false,
				Durable = true,
				Exclusive = false
			};
		}
	}
}
