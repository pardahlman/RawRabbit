using System;
using RabbitMQ.Client;
using RawRabbit.Context;

namespace RawRabbit.Extensions.BulkGet.Model
{
    public class BulkMessage<TMessage, TMessageContext> : IDisposable, IBulkMessage where TMessageContext : IMessageContext
    {
        private readonly IModel _channel;
        private readonly ulong _deliveryTag;

        public BulkMessage(IModel channel, ulong deliveryTag, TMessageContext context, object message)
        {
            _channel = channel;
            _deliveryTag = deliveryTag;
            Context = context;
            Message = (TMessage) message;
        }

        public TMessageContext Context { get; private set; }
        public TMessage Message { get; private set; }

        public void Ack()
        {
            _channel.BasicAck(_deliveryTag, false);
        }

        public void Nack(bool requeue = true)
        {
            _channel.BasicNack(_deliveryTag, false, requeue);
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}