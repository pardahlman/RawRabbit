using System.Collections.Generic;

namespace RawRabbit.Configuration.Queue
{
	public class QueueDeclaration
	{
		public string Name { get; set; }
		public bool Durable { get; set; }
		public bool Exclusive { get; set; }
		public bool AutoDelete { get; set; }
		public Dictionary<string, object> Arguments { get; set; }

		public QueueDeclaration()
		{
			Arguments = new Dictionary<string, object>();
		}

		public QueueDeclaration(GeneralQueueConfiguration cfg) : this()
		{
			Durable = cfg.Durable;
			AutoDelete = cfg.AutoDelete;
			Exclusive = cfg.Exclusive;
		}

		public static QueueDeclaration Default => new QueueDeclaration { };
	}
}
