using RabbitMQ.Client.Events;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.Context.Enhancer
{
    public interface IContextEnhancer
    {
        void WireUpContextFeatures<TMessageContext >(TMessageContext context, IRawConsumer consumer, BasicDeliverEventArgs args) where TMessageContext : IMessageContext;
    }
}