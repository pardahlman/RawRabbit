using System;
using System.Collections.Generic;
using RawRabbit.Operations.Saga.Model;

namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public class PhoneCallTriggers : TriggerConfiguration<Trigger>
	{
		public override List<TriggerInvoker> ConfigureTriggers(TriggerBuilder<Trigger> trigger)
		{
			trigger.Configure(Trigger.DialSignalSent)
				.FromMessage<DialSignalSent>(sent => sent.CallId);

			trigger.Configure(Trigger.CallDialed)
				.FromMessage<PhoneCallDialed>(dialed => dialed.CallId);

			trigger.Configure(Trigger.CallConnected)
				.FromMessage<PhonePickedUp>(up => up.CallId)
				.FromTimeSpan(TimeSpan.FromSeconds(5));

			trigger.Configure(Trigger.HungUp)
				.FromMessage<PhoneRinging>(ringing => ringing.CallId);

			return trigger.Build();
		}
	}
}
