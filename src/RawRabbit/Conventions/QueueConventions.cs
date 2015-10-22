using System;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Conventions
{
	public interface IQueueConventions
	{
		QueueConfiguration CreateQueueConfiguration<T>() where T: MessageBase;
		QueueConfiguration CreateResponseQueueConfiguration<T>() where T : MessageBase;
	}

	public class QueueConventions : IQueueConventions
	{
		public QueueConfiguration CreateQueueConfiguration<T>() where T: MessageBase
		{
			return new QueueConfiguration
			{
				QueueName = typeof (T).Name.ToLower(), //TODO: add something here to make each client unique
				AutoDelete = false,
				Durable = true,
				Exclusive = false
			};
		}

		public QueueConfiguration CreateResponseQueueConfiguration<T>() where T : MessageBase
		{
			return new QueueConfiguration
			{
				QueueName = "default_rpc_response." + Guid.NewGuid(),
				Exclusive = true,
				AutoDelete = true,
				Durable = false
			};
		}
	}
}
