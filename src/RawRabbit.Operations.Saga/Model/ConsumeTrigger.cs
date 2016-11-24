using System;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class ExternalTrigger { }

	public class ConsumeTrigger : ExternalTrigger
	{
		public ConsumerConfiguration Configuration { get; set; }
	}

	public class MessageTypeTrigger : ExternalTrigger
	{
		public Type MessageType { get; set; }
	}
}