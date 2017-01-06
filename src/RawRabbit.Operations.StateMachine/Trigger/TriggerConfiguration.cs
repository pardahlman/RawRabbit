using System.Collections.Generic;
using RawRabbit.Operations.StateMachine.Middleware;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public abstract class TriggerConfiguration
	{
		public abstract List<TriggerPipeOptions> GetTriggerPipeOptions();
	}

	public abstract class TriggerConfiguration<TStateMacahine> : TriggerConfiguration where TStateMacahine : StateMachineBase
	{
		public override List<TriggerPipeOptions> GetTriggerPipeOptions()
		{
			return ConfigureTriggers(new TriggerConfigurer<TStateMacahine>());
		}

		public abstract List<TriggerPipeOptions> ConfigureTriggers(TriggerConfigurer<TStateMacahine> trigger);
	}
}