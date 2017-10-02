using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Framing.Impl;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class ProduceAttributeOptions
	{
		public Func<IPipeContext, PublisherConfiguration> PublishConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ProduceAttributeMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, PublisherConfiguration> PublishConfigFunc;
		protected Func<IPipeContext, Type> MessageType;
		public override string StageMarker => Pipe.StageMarker.PublishConfigured;

		public ProduceAttributeMiddleware(ProduceAttributeOptions options = null)
		{
				PublishConfigFunc = options?.PublishConfigFunc ?? (context => context.GetPublishConfiguration());
				MessageType = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var publishConfig = GetPublishConfig(context);
			var messageType = GetMessageType(context);
			UpdateExchangeConfig(publishConfig, messageType);
			UpdateRoutingConfig(publishConfig, messageType);
			return Next.InvokeAsync(context, token);
		}

		protected virtual PublisherConfiguration GetPublishConfig(IPipeContext context)
		{
			return PublishConfigFunc?.Invoke(context);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return MessageType?.Invoke(context);
		}

		protected virtual void UpdateExchangeConfig(PublisherConfiguration config, Type messageType)
		{
			var attribute = GetAttribute<ExchangeAttribute>(messageType);
			if (attribute == null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				config.ExchangeName = attribute.Name;
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
				config.Exchange.AutoDelete= attribute.NullableAutoDelete.Value;
			}
			if (attribute.Type != ExchangeType.Unknown)
			{
				config.Exchange.ExchangeType = attribute.Type.ToString().ToLowerInvariant();
			}
		}

		protected virtual void UpdateRoutingConfig(PublisherConfiguration config, Type messageType)
		{
			var routingAttr = GetAttribute<RoutingAttribute>(messageType);
			if (routingAttr?.RoutingKey != null)
			{
				config.RoutingKey = routingAttr.RoutingKey;
			}
		}

		protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
		{
			var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
			return attr;
		}
	}
}
