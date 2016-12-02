using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class TriggerInvoker
	{
		public object Trigger { get; set; }
	}

	public class ConsumeTriggerInvoker : TriggerInvoker
	{
		public ConsumeConfiguration Configuration { get; set; }
	}

	public class MessageTriggerInvoker : TriggerInvoker
	{
		public Type MessageType { get; set; }
		public Action<IConsumerConfigurationBuilder> ConfigurationAction { get; set; }
	}
}