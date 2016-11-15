using System;
using RabbitMQ.Client;

namespace RawRabbit.Operations.Get.Model
{
	public class AckableGetResult : BasicGetResult, IDisposable
	{
		private readonly IModel _channel;

		public AckableGetResult(IModel channel, BasicGetResult result)
			: base(result.DeliveryTag, result.Redelivered, result.Exchange, result.RoutingKey, result.MessageCount, result.BasicProperties, result.Body)
		{
			_channel = channel;
		}

		public void Ack()
		{
			_channel.BasicAck(DeliveryTag, false);
		}

		public void Nack(bool requeue = true)
		{
			_channel.BasicNack(DeliveryTag, false, requeue);
		}

		public void Reject(bool requeue = true)
		{
			_channel.BasicReject(DeliveryTag, requeue);
		}

		public void Dispose()
		{
			_channel.Dispose();
		}
	}
}
