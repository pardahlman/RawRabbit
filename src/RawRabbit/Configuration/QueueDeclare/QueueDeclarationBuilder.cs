using RawRabbit.Common;

namespace RawRabbit.Configuration.Queue
{
	public class QueueDeclarationBuilder : IQueueDeclarationBuilder
	{
		public QueueDeclaration Declaration { get;}

		public QueueDeclarationBuilder(QueueDeclaration initialQueue = null)
		{
			Declaration = initialQueue ?? QueueDeclaration.Default;
		}

		public IQueueDeclarationBuilder WithName(string queueName)
		{
			Truncator.Truncate(ref queueName);
			Declaration.Name = queueName;
			return this;
		}

		public IQueueDeclarationBuilder WithNameSuffix(string suffix)
		{
			WithName($"{Declaration.Name}_{suffix}");
			return this;
		}

		public IQueueDeclarationBuilder WithAutoDelete(bool autoDelete = true)
		{
			Declaration.AutoDelete = autoDelete;
			return this;
		}

		public IQueueDeclarationBuilder WithDurability(bool durable = true)
		{
			Declaration.Durable = durable;
			return this;
		}

		public IQueueDeclarationBuilder WithExclusivity(bool exclusive = true)
		{
			Declaration.Exclusive = exclusive;
			return this;
		}

		public IQueueDeclarationBuilder WithArgument(string key, object value)
		{
			Declaration.Arguments.Add(key, value);
			return this;
		}
	}
}
