using System.Collections.Generic;
using System.Linq;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class TriggerConfiguration
	{
		public abstract Dictionary<object, List<ExternalTrigger>> ConfigureTriggers();
	}

	public abstract class TriggerConfiguration<TTrigger> : TriggerConfiguration
	{
		public override Dictionary<object, List<ExternalTrigger>> ConfigureTriggers()
		{
			var triggers = ConfigureTriggers(new TriggerBuilder<TTrigger>());
			return triggers.ToDictionary(t => t.Key as object, t => t.Value);
		}

		public abstract Dictionary<TTrigger, List<ExternalTrigger>> ConfigureTriggers(TriggerBuilder<TTrigger> trigger);
	}
}