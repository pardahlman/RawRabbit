using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Legacy.Respond;

namespace RawRabbit.Consumer.Abstraction
{
	public interface IRawConsumerFactory
	{
		IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel);
		IRawConsumer CreateConsumer(ConsumeConfiguration cfg, IModel channel);
	}
}
