using System;
using RabbitMQ.Client;
using RawRabbit.Consumer;

namespace RawRabbit.Subscription
{
	public interface ISubscription : IDisposable
	{
		string QueueName { get; }
		string ConsumerTag { get; }
		bool Active { get;  }
	}

	public class Subscription : ISubscription
	{
		public string QueueName { get; }
		public string ConsumerTag { get; }
		public bool Active { get; set; }

		private readonly IBasicConsumer _consumer;

		public Subscription(IBasicConsumer consumer, string queueName)
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
			if (!_consumer.Model.IsOpen)
			{
				return;
			}
			if (!Active)
			{
				return;
			}
			Active = false;
			_consumer.CancelAsync();
		}
	}
}
