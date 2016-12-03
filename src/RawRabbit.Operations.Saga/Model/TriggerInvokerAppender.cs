using System;
using System.Collections.Generic;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;

namespace RawRabbit.Operations.Saga.Model
{
	public class TriggerInvokerAppender
	{
		public object Trigger { get; set; }

		public List<TriggerInvoker> TriggerInvokers { get; }

		public TriggerInvokerAppender(object trigger)
		{
			Trigger = trigger;
			TriggerInvokers = new List<TriggerInvoker>();
		}
		public TriggerInvokerAppender From(TriggerInvoker invoker)
		{
			invoker.Trigger = Trigger;
			TriggerInvokers.Add(invoker);
			return this;
		}
	}

	public static class TriggerAppenderExtensions
	{
		public static TriggerInvokerAppender FromQueue(this TriggerInvokerAppender appender, string queueName, Func<BasicDeliverEventArgs, Guid> correlation,  bool noAck = false)
		{
			appender.From(new ConsumeTriggerInvoker
			{
				CorrelationFunc = o => correlation?.Invoke((BasicDeliverEventArgs)o) ?? Guid.Empty,
				Configuration = new ConsumeConfiguration
				{
					QueueName = queueName,
					NoAck = noAck
				}
			});
			return appender;
		}

		public static TriggerInvokerAppender FromMessage<TMessage>(this TriggerInvokerAppender appender, Func<TMessage, Guid> correlation, Action<IConsumerConfigurationBuilder> config = null)
		{
			return appender.From(new MessageTriggerInvoker
			{
				MessageType = typeof(TMessage),
				ConfigurationAction = config,
				CorrelationFunc = o => correlation?.Invoke((TMessage)o) ?? Guid.Empty
			});
		}

		public static TriggerInvokerAppender FromTimeSpan(this TriggerInvokerAppender appender, TimeSpan timer, bool recurring = false)
		{
			return appender;
		}
	}
}