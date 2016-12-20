using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Consumer
{
	public interface IConsumerFactory
	{
		Task<IBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg, IModel channel = null);
		Task<IBasicConsumer> CreateConsumerAsync(IModel channel = null);
		IBasicConsumer ConfigureConsume(IBasicConsumer consumer, ConsumeConfiguration cfg);
	}
}