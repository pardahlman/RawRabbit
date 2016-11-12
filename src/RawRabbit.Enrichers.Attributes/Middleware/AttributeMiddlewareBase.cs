using System;
using System.Reflection;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public abstract class AttributeMiddlewareBase : StagedMiddleware
	{
		protected virtual void UpdateExchangeConfig(ExchangeConfiguration exchange, Type type)
		{
			var exchangeAttr = GetAttribute<ExchangeAttribute>(type);
			if (exchangeAttr == null)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(exchangeAttr.Name))
			{
				exchange.ExchangeName = exchangeAttr.Name;
			}
			if (exchangeAttr.NullableDurability.HasValue)
			{
				exchange.Durable = exchangeAttr.NullableDurability.Value;
			}
			if (exchangeAttr.NullableAutoDelete.HasValue)
			{
				exchange.AutoDelete = exchangeAttr.NullableAutoDelete.Value;
			}
			if (exchangeAttr.Type != ExchangeType.Unknown)
			{
				exchange.ExchangeType = exchangeAttr.Type.ToString().ToLower();
			}
		}

		protected virtual void UpdateQueueConfig(QueueConfiguration queue, Type type)
		{
			var queueAttr = GetAttribute<QueueAttribute>(type);
			if (queueAttr == null)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.Name))
			{
				queue.QueueName = queueAttr.Name;
			}
			if (queueAttr.NullableDurability.HasValue)
			{
				queue.Durable = queueAttr.NullableDurability.Value;
			}
			if (queueAttr.NullableExclusitivy.HasValue)
			{
				queue.Exclusive = queueAttr.NullableExclusitivy.Value;
			}
			if (queueAttr.NullableAutoDelete.HasValue)
			{
				queue.Durable = queueAttr.NullableAutoDelete.Value;
			}
			if (queueAttr.MessageTtl > 0)
			{
				queue.Arguments.Add(QueueArgument.MessageTtl, queueAttr.MessageTtl);
			}
			if (queueAttr.MaxPriority > 0)
			{
				queue.Arguments.Add(QueueArgument.MaxPriority, queueAttr.MaxPriority);
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.DeadLeterExchange))
			{
				queue.Arguments.Add(QueueArgument.DeadLetterExchange, queueAttr.DeadLeterExchange);
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.Mode))
			{
				queue.Arguments.Add(QueueArgument.QueueMode, queueAttr.Mode);
			}
		}

		protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
		{
			var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
			return attr;
		}
	}
}
