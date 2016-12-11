namespace RawRabbit.Operations.MessageSequence.Model
{
	public class StepOption
	{
		public bool AbortsExecution { get; set; }
		public bool Optional { get; set; }

		public static StepOption Default => new StepOption
		{
			Optional = false,
			AbortsExecution = false
		};
	}
}