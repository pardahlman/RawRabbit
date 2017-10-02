using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;

namespace RawRabbit.Operations.MessageSequence.Configuration
{
	public class StepOptionBuilder : IStepOptionBuilder
	{
		public StepOption Configuration { get; set; }

		public StepOptionBuilder()
		{
			Configuration = StepOption.Default;
		}

		public IStepOptionBuilder AbortsExecution(bool aborts = true)
		{
			Configuration.Optional = true;
			Configuration.AbortsExecution = aborts;
			return this;
		}

		public IStepOptionBuilder IsOptional(bool optional = true)
		{
			Configuration.Optional = optional;
			return this;
		}
	}
}