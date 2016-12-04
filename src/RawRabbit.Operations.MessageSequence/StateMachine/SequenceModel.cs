using System.Collections.Generic;
using RawRabbit.Context;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.Saga.Model;

namespace RawRabbit.Operations.MessageSequence.StateMachine
{
	public class SequenceModel : SagaModel<SequenceState>
	{
		public bool Aborted { get; set; }
		public List<ExecutionResult> Completed { get; set; }
		public List<ExecutionResult> Skipped { get; set; }
	}
}
