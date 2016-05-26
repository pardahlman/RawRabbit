using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RawRabbit.Consumer.Abstraction
{
	public interface IRawConsumer : IBasicConsumer
	{
		Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }
		List<ulong> AcknowledgedTags { get; }
		void Disconnect();
	}
}