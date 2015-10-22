using RawRabbit.Configuration.Operation;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Request
{
	public class RequestConfiguration : ConfigurationBase
	{
		/*
			ReplyQueue is a proxy property for Queue, which makes
			it easier to follow the flow in Requester
		*/ 
		public QueueConfiguration ReplyQueue
		{
			get { return Queue; }
			set { Queue = value; }
		}
	}
}
