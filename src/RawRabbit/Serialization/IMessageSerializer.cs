using System;
using RabbitMQ.Client.Events;

namespace RawRabbit.Serialization
{
	public interface IMessageSerializer
	{
		byte[] Serialize<T>(T obj);
		T Deserialize<T>(byte[] bytes);
		object Deserialize(byte[] bytes, Type messageType);
		object Deserialize(BasicDeliverEventArgs args);
	}
}
