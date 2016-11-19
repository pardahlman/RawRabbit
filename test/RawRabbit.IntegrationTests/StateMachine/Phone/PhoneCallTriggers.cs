using System;
using System.Collections.Generic;
using RawRabbit.Operations.Saga.Model;

namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public class PhoneCallTriggers : TriggerConfiguration<Trigger>
	{
		public override Dictionary<Trigger, List<ExternalTrigger>> ConfigureTriggers(TriggerBuilder<Trigger> trigger)
		{
			trigger.Configure(Trigger.DialSignalSent)
				.FromMessage<DialSignalSent>();

			trigger.Configure(Trigger.CallDialed)
				.FromMessage<PhoneCallDialed>();

			trigger.Configure(Trigger.CallConnected)
				.FromMessage<PhonePickedUp>()
				.FromTimeSpan(TimeSpan.FromSeconds(5));

			trigger.Configure(Trigger.HungUp)
				.FromMessage<PhoneRinging>();

			return trigger.Build();
		}
	}
}
