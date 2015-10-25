using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RawRabbit.Consumer.Contract
{
	public interface IRawConsumer : IBasicConsumer
	{
		Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }
		void Disconnect();
	}
}