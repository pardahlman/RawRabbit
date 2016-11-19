using System.Collections.Generic;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class TriggerDefinition<TTrigger>
	{
		public Dictionary<TTrigger, List<ExternalTrigger>> Type { get; set; }
	}
}
