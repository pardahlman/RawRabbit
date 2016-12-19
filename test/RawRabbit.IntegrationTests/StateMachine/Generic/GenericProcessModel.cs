using System;
using RawRabbit.Operations.Saga.Model;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class GenericProcessModel : SagaModel<State>
	{
		public string Assignee { get; set; }
		public string Name { get; set; }
		public DateTime Deadline { get; set; }
	}
}
