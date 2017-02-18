using System;
using System.Collections.Generic;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public class TriggerConfigurer
	{
		public List<Action<IPipeContext>> TriggerContextActions { get; set; }
		
		public TriggerConfigurer()
		{
			TriggerContextActions = new List<Action<IPipeContext>>();
		}

		public TriggerConfigurer From(Action<IPipeContext> context)
		{
			TriggerContextActions.Add(context);
			return this;
		}

	}
}
