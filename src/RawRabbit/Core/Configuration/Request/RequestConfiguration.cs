using RawRabbit.Core.Configuration.Operation;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Request
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
