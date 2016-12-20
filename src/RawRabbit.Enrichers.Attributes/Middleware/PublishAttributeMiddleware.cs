using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class PublishAttributeOptions
	{
		public Func<IPipeContext, PublisherConfiguration> ConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class PublishAttributeMiddleware : AttributeMiddlewareBase
	{
		private readonly Func<IPipeContext, PublisherConfiguration> _configFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public PublishAttributeMiddleware(PublishAttributeOptions options = null)
		{
			_configFunc = options?.ConfigFunc ?? (context => context.GetPublishConfiguration());
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
			UpdateRoutingConfig(config, msgType);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void UpdateRoutingConfig(PublisherConfiguration config, Type type)
		{
			var routingAttr = GetAttribute<RoutingAttribute>(type);
			if (routingAttr?.RoutingKey != null)
			{
				config.RoutingKey =  routingAttr.RoutingKey;
			}
		}

		public override string StageMarker => Pipe.StageMarker.PublishConfigured;
	}
}
