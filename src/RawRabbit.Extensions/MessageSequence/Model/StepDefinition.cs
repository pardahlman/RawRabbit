using System;
using System.Threading.Tasks;
using RawRabbit.Context;

namespace RawRabbit.Extensions.MessageSequence.Model
{
	public class StepDefinition
	{
		public Func<object, IMessageContext, Task> Handler { get; set; }
		public Type Type { get; set; }
		public bool Optional { get; set; }
		public bool AbortsExecution { get; set; }
	}
}