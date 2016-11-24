using RabbitMQ.Client;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Legacy.Respond;

namespace RawRabbit.Consumer.Abstraction
{
	public interface IRawConsumerFactory
	{
		IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel);
		IRawConsumer CreateConsumer(ConsumerConfiguration cfg, IModel channel);
	}
}
