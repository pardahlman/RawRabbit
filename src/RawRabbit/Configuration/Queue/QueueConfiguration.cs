using System.Collections.Generic;

namespace RawRabbit.Configuration.Queue
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

		public QueueConfiguration(GeneralQueueConfiguration cfg) : this()
		{
			Durable = cfg.Durable;
			AutoDelete = cfg.AutoDelete;
			Exclusive = cfg.Exclusive;
		}

		public static QueueConfiguration Default => new QueueConfiguration { };		
	}
}
