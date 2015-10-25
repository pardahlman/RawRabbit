using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Consumer
{
	class EventingRawConsumer : EventingBasicConsumer, IRawConsumer
	{
		public EventingRawConsumer(IModel channel) : base(channel)
		{
		}

		public Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }

		public void Disconnect()
		{
			Model.BasicCancel(ConsumerTag);
		}
	}
}
