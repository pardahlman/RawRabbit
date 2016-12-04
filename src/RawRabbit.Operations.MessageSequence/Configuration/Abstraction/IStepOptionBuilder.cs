namespace RawRabbit.Operations.MessageSequence.Configuration.Abstraction
{
	public interface IStepOptionBuilder
	{
		IStepOptionBuilder AbortsExecution(bool aborts = true);
		IStepOptionBuilder IsOptional(bool optional = true);
	}
}