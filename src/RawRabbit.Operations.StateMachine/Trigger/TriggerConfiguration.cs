using System;
using System.Collections.Generic;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public abstract class TriggerConfiguration
	{
		public List<Action<IPipeContext>> GetTriggerContextActions()
		{
			var configurer = new TriggerConfigurer();
			ConfigureTriggers(configurer);
			return configurer.TriggerContextActions;
		}

		public abstract void ConfigureTriggers(TriggerConfigurer trigger);
	}
}