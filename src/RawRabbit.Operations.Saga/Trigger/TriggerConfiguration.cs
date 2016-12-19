using System.Collections.Generic;
using RawRabbit.Operations.Saga.Middleware;

namespace RawRabbit.Operations.Saga.Trigger
{
	public abstract class TriggerConfiguration
	{
		public abstract List<SagaSubscriberOptions> GetSagaSubscriberOptions();
	}

	public abstract class TriggerConfiguration<TSaga> : TriggerConfiguration where TSaga : Model.Saga
	{
		public override List<SagaSubscriberOptions> GetSagaSubscriberOptions()
		{
			return ConfigureTriggers(new TriggerConfigurer<TSaga>());
		}

		public abstract List<SagaSubscriberOptions> ConfigureTriggers(TriggerConfigurer<TSaga> trigger);
	}
}