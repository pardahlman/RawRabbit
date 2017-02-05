using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class ConsumeAttributeOptions
	{
		public Func<IPipeContext, ConsumerConfiguration> PublishConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ConsumeAttributeMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;
		protected Func<IPipeContext, ConsumerConfiguration> ConsumeConfigFunc;
		protected Func<IPipeContext, Type> MessageType;

		public ConsumeAttributeMiddleware(ConsumeAttributeOptions options = null)
		{
			ConsumeConfigFunc = options?.PublishConfigFunc ?? (context => context.GetConsumerConfiguration());
			MessageType = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var consumeConfig = GetConsumerConfig(context);
			var messageType = GetMessageType(context);
			UpdateExchangeConfig(consumeConfig, messageType);
			UpdateRoutingConfig(consumeConfig, messageType);
			UpdateQueueConfig(consumeConfig, messageType);
			return Next.InvokeAsync(context, token);
		}

		protected virtual ConsumerConfiguration GetConsumerConfig(IPipeContext context)
		{
			return ConsumeConfigFunc?.Invoke(context);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return MessageType?.Invoke(context);
		}

		protected virtual void UpdateExchangeConfig(ConsumerConfiguration config, Type messageType)
		{
			var attribute = GetAttribute<ExchangeAttribute>(messageType);
			if (attribute == null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				config.Consume.ExchangeName = attribute.Name;
				if (config.Exchange != null)
				{
					config.Exchange.Name = attribute.Name;
				}
			}
			if (config.Exchange == null)
			{
				return;
			}
			if (attribute.NullableDurability.HasValue)
			{
				config.Exchange.Durable = attribute.NullableDurability.Value;
			}
			if (attribute.NullableAutoDelete.HasValue)
			{
				config.Exchange.AutoDelete = attribute.NullableAutoDelete.Value;
			}
			if (attribute.Type != ExchangeType.Unknown)
			{
				config.Exchange.ExchangeType = attribute.Type.ToString().ToLowerInvariant();
			}
		}

		protected virtual void UpdateRoutingConfig(ConsumerConfiguration config, Type messageType)
		{
			var routingAttr = GetAttribute<RoutingAttribute>(messageType);
			if (routingAttr == null)
			{
				return;
			}
			if (routingAttr?.RoutingKey != null)
			{
				config.Consume.RoutingKey = routingAttr.RoutingKey;
			}
			if (routingAttr.PrefetchCount != 0)
			{
				config.Consume.PrefetchCount = routingAttr.PrefetchCount;
			}
			if (routingAttr.NullableNoAck.HasValue)
			{
				config.Consume.NoAck = routingAttr.NoAck;
			}
		}

		protected virtual void UpdateQueueConfig(ConsumerConfiguration config, Type messageType)
		{
			var attribute = GetAttribute<QueueAttribute>(messageType);
			if (attribute == null)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				config.Consume.QueueName = attribute.Name;
				if (config.Queue != null)
				{
					config.Queue.Name = attribute.Name;
				}
			}
			if (config.Queue == null)
			{
				return;
			}
			if (attribute.NullableDurability.HasValue)
			{
				config.Queue.Durable = attribute.Durable;
			}
			if (attribute.NullableExclusitivy.HasValue)
			{
				config.Queue.Exclusive = attribute.Exclusive;
			}
			if (attribute.NullableAutoDelete.HasValue)
			{
				config.Queue.AutoDelete= attribute.AutoDelete;
			}
			if (attribute.MessageTtl > 0)
			{
				config.Queue.Arguments.AddOrReplace(QueueArgument.MessageTtl, attribute.MessageTtl);
			}
			if (attribute.MaxPriority > 0)
			{
				config.Queue.Arguments.AddOrReplace(QueueArgument.MaxPriority, attribute.MaxPriority);
			}
			if (!string.IsNullOrWhiteSpace(attribute.DeadLeterExchange))
			{
				config.Queue.Arguments.AddOrReplace(QueueArgument.DeadLetterExchange, attribute.DeadLeterExchange);
			}
			if (!string.IsNullOrWhiteSpace(attribute.Mode))
			{
				config.Queue.Arguments.AddOrReplace(QueueArgument.QueueMode, attribute.Mode);
			}
		}

		protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
		{
			var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
			return attr;
		}
	}
}
