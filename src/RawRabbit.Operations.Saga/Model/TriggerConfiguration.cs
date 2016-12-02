using System.Collections.Generic;
using System.Linq;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class TriggerConfiguration
	{
		public abstract List<TriggerInvoker> GetTriggerInvokers();
	}

	public abstract class TriggerConfiguration<TTrigger> : TriggerConfiguration
	{
		public override List<TriggerInvoker> GetTriggerInvokers()
		{
			return ConfigureTriggers(new TriggerBuilder<TTrigger>());
		}

		public abstract List<TriggerInvoker> ConfigureTriggers(TriggerBuilder<TTrigger> trigger);
	}
}