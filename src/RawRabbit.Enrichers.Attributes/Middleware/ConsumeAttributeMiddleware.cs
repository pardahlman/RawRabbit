using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class ConsumeAttributeOptions
	{
		public Func<IPipeContext, ConsumeConfiguration> ConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ConsumeAttributeMiddleware : AttributeMiddlewareBase
	{
		private readonly Func<IPipeContext, ConsumeConfiguration> _configFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public ConsumeAttributeMiddleware(ConsumeAttributeOptions options = null)
		{
			_configFunc = options?.ConfigFunc ?? (context => context.GetConsumerConfiguration());
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context)
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
			UpdateRouting(config, msgType);
			return Next.InvokeAsync(context);
		}

		private void UpdateRouting(ConsumeConfiguration config, Type type)
		{
			var routingAttr = GetAttribute<RoutingAttribute>(type);
			if (routingAttr?.RoutingKey != null)
			{
				config.RoutingKey = routingAttr.RoutingKey;
			}
			if (routingAttr?.NullableNoAck != null)
			{
				config.NoAck = routingAttr.NoAck;
			}
		}

		public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;
	}
}
