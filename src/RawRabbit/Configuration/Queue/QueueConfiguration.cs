using System.Collections.Generic;

namespace RawRabbit.Configuration.Queue
{
	public class QueueConfiguration
	{
		public string FullQueueName
		{
			get
			{
				var fullQueueName =  string.IsNullOrEmpty(NameSuffix)
					? QueueName
					: $"{QueueName}_{NameSuffix}";

				return fullQueueName.Length > 254
					? string.Concat("...", fullQueueName.Substring(fullQueueName.Length - 250))
					: fullQueueName;
			}
		}

		public string QueueName { get; set; }
		public string NameSuffix { get; set; }
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
