using System.Linq;

namespace RawRabbit.Extensions.BulkGet.Configuration
{
	public interface IMessageConfigurationBuilder
	{
		IMessageConfigurationBuilder FromQueues(params string[] queueNames);
		IMessageConfigurationBuilder GetAll();
		IMessageConfigurationBuilder WithBatchSize(int batchSize);
		IMessageConfigurationBuilder WithNoAck(bool noAck = true);
	}

	public class MessageConfigurationBuilder<TMessage> : IMessageConfigurationBuilder
	{
		public MessageConfiguration Configuration { get; }

		public MessageConfigurationBuilder()
		{
			Configuration = new MessageConfiguration
			{
				MessageType = typeof(TMessage)
			};
		}
		public IMessageConfigurationBuilder FromQueues(params string[] queueNames)
		{
			Configuration.QueueNames = queueNames.ToList();
			return this;
		}

		public IMessageConfigurationBuilder GetAll()
		{
			Configuration.GetAll = true;
			return this;
		}

		public IMessageConfigurationBuilder WithBatchSize(int batchSize)
		{
			Configuration.BatchSize = batchSize;
			return this;
		}

		public IMessageConfigurationBuilder WithNoAck(bool noAck = true)
		{
			Configuration.NoAck = noAck;
			return this;
		}
	}
}