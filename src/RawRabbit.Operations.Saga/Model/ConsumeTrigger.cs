using System;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class ExternalTrigger { }

	public class ConsumeTrigger : ExternalTrigger
	{
		public ConsumeConfiguration Configuration { get; set; }
	}

	public class MessageTypeTrigger : ExternalTrigger
	{
		public Type MessageType { get; set; }
	}
}