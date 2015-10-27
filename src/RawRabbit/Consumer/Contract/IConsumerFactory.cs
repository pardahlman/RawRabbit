using RabbitMQ.Client;
using RawRabbit.Configuration.Respond;

namespace RawRabbit.Consumer.Contract
{
	public interface IConsumerFactory
	{
		IRawConsumer CreateConsumer(IConsumerConfiguration cfg);
		IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel);
	}
}
