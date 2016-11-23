using RabbitMQ.Client;
using RawRabbit.Configuration.Respond;

namespace RawRabbit.Consumer.Abstraction
{
    public interface IConsumerFactory
    {
        IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel);
    }
}
