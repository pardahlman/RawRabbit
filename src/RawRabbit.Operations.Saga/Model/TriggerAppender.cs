using System;
using System.Collections.Generic;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Operations.Saga.Model
{
	public class TriggerAppender
	{
		public object Trigger { get; set; }

		public List<ExternalTrigger> ExternalTriggers { get; }

		public TriggerAppender(object trigger)
		{
			Trigger = trigger;
			ExternalTriggers = new List<ExternalTrigger>();
		}
		public TriggerAppender From(ExternalTrigger trigger)
		{
			ExternalTriggers.Add(trigger);
			return this;
		}
	}

	public static class TriggerAppenderExtensions
	{
		public static TriggerAppender FromQueue(this TriggerAppender appender, string queueName, bool noAck = false)
		{
			appender.From(new ConsumeTrigger
			{
				Configuration = new ConsumerConfiguration
				{
					Queue = new QueueDeclaration
					{
						Name = queueName
					}
				}
			});
			return appender;
		}

		public static TriggerAppender FromMessage<TMessage>(this TriggerAppender appender, Action<IConsumerConfigurationBuilder> config = null)
		{
			return appender.From(new MessageTypeTrigger
			{
				MessageType = typeof(TMessage),
				ConfigurationAction = config
			});
		}

		public static TriggerAppender FromTimeSpan(this TriggerAppender appender, TimeSpan timer, bool recurring = false)
		{
			return appender;
		}
	}
}