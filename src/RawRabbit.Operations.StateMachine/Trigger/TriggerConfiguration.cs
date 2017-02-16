using System;
using System.Collections.Generic;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public abstract class TriggerConfiguration
	{
		public abstract List<Action<IPipeContext>> GetTriggerContextActions();
	}

	public abstract class TriggerConfiguration<TStateMacahine> : TriggerConfiguration where TStateMacahine : StateMachineBase
	{
		public override List<Action<IPipeContext>> GetTriggerContextActions()
		{
			var configurer = new TriggerConfigurer<TStateMacahine>();
			ConfigureTriggers(configurer);
			return configurer.TriggerContextActions;
		}

		public abstract void ConfigureTriggers(TriggerConfigurer<TStateMacahine> trigger);
	}
}