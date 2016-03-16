using RawRabbit.Extensions.MessageSequence.Configuration.Abstraction;
using RawRabbit.Extensions.MessageSequence.Model;

namespace RawRabbit.Extensions.MessageSequence.Configuration
{
	public class StepOptionBuilder : IStepOptionBuilder
	{
		public StepOption Configuration { get; set; }

		public StepOptionBuilder()
		{
			Configuration = new StepOption();
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