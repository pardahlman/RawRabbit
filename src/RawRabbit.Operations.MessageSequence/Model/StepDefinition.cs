using System;
using System.Threading.Tasks;

namespace RawRabbit.Operations.MessageSequence.Model
{
	public class StepDefinition
	{
		public Guid Id { get; private set; }
		public Type Type { get; set; }
		public bool Optional { get; set; }
		public bool AbortsExecution { get; set; }

		public StepDefinition()
		{
			Id = Guid.NewGuid();
		}
	}
}