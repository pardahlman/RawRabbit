using System.Collections.Generic;

namespace RawRabbit.Core.Configuration.Queue
{
	public class QueueConfiguration
	{
		public string QueueName { get; set; }
		public bool Durable { get; set; }
		public bool Exclusive { get; set; }
		public bool AutoDelete { get; set; }
		public Dictionary<string, object> Arguments { get; set; }

		public QueueConfiguration()
		{
			Arguments = new Dictionary<string, object>();
		}

		public static QueueConfiguration Default => new QueueConfiguration
		{
		};

		
	}
}
