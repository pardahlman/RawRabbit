using System.Collections.Generic;

namespace RawRabbit.Configuration.Queue
{
	public class QueueDeclaration
	{
		public string FullQueueName
		{
			get
			{
				var fullQueueName =  string.IsNullOrEmpty(NameSuffix)
					? Name
					: $"{Name}_{NameSuffix}";

				return fullQueueName.Length > 254
					? string.Concat("...", fullQueueName.Substring(fullQueueName.Length - 250))
					: fullQueueName;
			}
		}

		public string Name { get; set; }
		public string NameSuffix { get; set; }
		public bool Durable { get; set; }
		public bool Exclusive { get; set; }
		public bool AutoDelete { get; set; }
		public Dictionary<string, object> Arguments { get; set; }
		public bool AssumeInitialized { get; set; }

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
