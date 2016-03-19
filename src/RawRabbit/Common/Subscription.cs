using System;
using RabbitMQ.Client;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.Common
{
	public interface ISubscription : IDisposable
	{
		string QueueName { get; }
		string ConsumerTag { get; }
	}

	public class Subscription : ISubscription
	{
		public string QueueName { get; }
		public string ConsumerTag { get; }

		private readonly IRawConsumer _consumer;

		public Subscription(IRawConsumer consumer, string queueName)
		{
			_consumer = consumer;
			var basicConsumer = consumer as DefaultBasicConsumer;
			if (basicConsumer == null)
			{
				return;
			}
			QueueName = queueName;
			ConsumerTag = basicConsumer.ConsumerTag;
		}

		public void Dispose()
		{
			_consumer.Disconnect();
		}
	}
}
