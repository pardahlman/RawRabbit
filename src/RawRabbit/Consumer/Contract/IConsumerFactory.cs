using RawRabbit.Configuration.Respond;

namespace RawRabbit.Consumer.Contract
{
	public interface IConsumerFactory
	{
		IRawConsumer CreateConsumer(IConsumerConfiguration cfg);
	}
}
