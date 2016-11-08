using RawRabbit.Configuration.Consume;

namespace RawRabbit.Operations.Respond.Configuration
{
	public interface IRespondConfigurationBuilder : IConsumeConfigurationBuilder
	{
	}

	public class RespondConfigurationBuilder : ConsumeConfigurationBuilder, IRespondConfigurationBuilder
	{
		public RespondConfigurationBuilder(ConsumeConfiguration initial) : base(initial)
		{
		}
	}
}
