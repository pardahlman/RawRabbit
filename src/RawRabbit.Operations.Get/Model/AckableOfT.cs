using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RawRabbit.Operations.Get.Model
{
	public class Ackable<TType> : IDisposable
	{
		public TType Content { get; set; }
		public bool Acknowledged { get; private set; }
		public IEnumerable<ulong> DeliveryTags => DeliveryTagFunc(Content);
		internal readonly IModel Channel;
		internal readonly Func<TType, ulong[]> DeliveryTagFunc;

		public Ackable(TType content, IModel channel, params ulong[] deliveryTag) : this(content, channel, type => deliveryTag)
		{ }

		public Ackable(TType content, IModel channel, Func<TType, ulong[]> deliveryTagFunc)
		{
			Content = content;
			Channel = channel;
			DeliveryTagFunc = deliveryTagFunc;
		}

		public void Ack()
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				Channel.BasicAck(deliveryTag, false);
			}
			Acknowledged = true;
		}

		public void Nack(bool requeue = true)
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				Channel.BasicNack(deliveryTag, false, requeue);
			}
			Acknowledged = true;
		}

		public void Reject(bool requeue = true)
		{
			foreach (var deliveryTag in DeliveryTagFunc(Content))
			{
				Channel.BasicReject(deliveryTag, requeue);
			}
			Acknowledged = true;
		}

		public void Dispose()
		{
			Channel?.Dispose();
		}
	}
}
