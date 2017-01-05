using RawRabbit.Common;

namespace RawRabbit.Configuration.Queue
{
	public class QueueDeclarationBuilder : IQueueDeclarationBuilder
	{
		public QueueDeclaration Configuration { get;}

		public QueueDeclarationBuilder(QueueDeclaration initialQueue = null)
		{
			Configuration = initialQueue ?? QueueDeclaration.Default;
		}

		public IQueueDeclarationBuilder WithName(string queueName)
		{
			Truncator.Truncate(ref queueName);
			Configuration.Name = queueName;
			return this;
		}

		public IQueueDeclarationBuilder WithNameSuffix(string suffix)
		{
			WithName($"{Configuration.Name}_{suffix}");
			return this;
		}

		public IQueueDeclarationBuilder WithAutoDelete(bool autoDelete = true)
		{
			Configuration.AutoDelete = autoDelete;
			return this;
		}

		public IQueueDeclarationBuilder WithDurability(bool durable = true)
		{
			Configuration.Durable = durable;
			return this;
		}

		public IQueueDeclarationBuilder WithExclusivity(bool exclusive = true)
		{
			Configuration.Exclusive = exclusive;
			return this;
		}

		public IQueueDeclarationBuilder WithArgument(string key, object value)
		{
			Configuration.Arguments.Add(key, value);
			return this;
		}
	}
}
