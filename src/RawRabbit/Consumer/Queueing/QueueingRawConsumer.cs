using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Consumer.Queueing
{
	class QueueingRawConsumer : QueueingBasicConsumer, IRawConsumer
	{
		public QueueingRawConsumer(IModel channel) : base(channel)
		{
			NackedDeliveryTags = new List<ulong>();
		}

		public Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }

		public List<ulong> NackedDeliveryTags { get; }

		public void Disconnect()
		{
			Model.BasicCancel(ConsumerTag);
		}
	}
}