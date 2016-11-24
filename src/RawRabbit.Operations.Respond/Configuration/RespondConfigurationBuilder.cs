using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Operations.Respond.Configuration
{
	public interface IRespondConfigurationBuilder : IConsumerConfigurationBuilder
	{
	}

	public class RespondConfigurationBuilder : ConsumerConfigurationBuilder, IRespondConfigurationBuilder
	{
		public RespondConfigurationBuilder(ConsumerConfiguration initial) : base(initial)
		{
		}
	}
}
