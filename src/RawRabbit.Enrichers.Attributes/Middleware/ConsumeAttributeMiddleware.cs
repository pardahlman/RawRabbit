using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class ConsumeAttributeOptions
	{
		public Func<IPipeContext, ConsumerConfiguration> ConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ConsumeAttributeMiddleware : AttributeMiddlewareBase
	{
		private readonly Func<IPipeContext, ConsumerConfiguration> _configFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public ConsumeAttributeMiddleware(ConsumeAttributeOptions options = null)
		{
			_configFunc = options?.ConfigFunc ?? (context => context.GetConsumerConfiguration());
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var msgType = _messageTypeFunc(context);
			var config = _configFunc(context);
			if (msgType == null)
			{
				throw new KeyNotFoundException(nameof(msgType));
			}
			if (config == null)
			{
				throw new KeyNotFoundException(nameof(config));
			}
			UpdateExchangeConfig(config.Exchange, msgType);
			UpdateQueueConfig(config.Queue, msgType);
			AlginConsumeProps(config);
			UpdateRouting(config, msgType);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void AlginConsumeProps(ConsumerConfiguration config)
		{
			config.Consume.ExchangeName = config.Exchange?.Name ?? config.Consume.ExchangeName;
			config.Consume.QueueName = config.Queue?.Name ?? config.Consume.QueueName;
		}

		private void UpdateRouting(ConsumerConfiguration config, Type type)
		{
			var routingAttr = GetAttribute<RoutingAttribute>(type);
			if (routingAttr?.RoutingKey != null)
			{
				config.Consume.RoutingKey = routingAttr.RoutingKey;
			}
			if (routingAttr?.NullableNoAck != null)
			{
				config.Consume.NoAck = routingAttr.NoAck;
			}
		}

		public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;
	}
}
