using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using IPublishConfigurationBuilder = RawRabbit.Configuration.Consume.IPublishConfigurationBuilder;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishConfigurationOptions
	{
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class PublishConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IPublishConfigurationFactory _publishFactory;
		private readonly Func<IPipeContext, string> _exchangeFunc;
		private readonly Func<IPipeContext, string> _routingKeyFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public PublishConfigurationMiddleware(IPublishConfigurationFactory publishFactory, PublishConfigurationOptions options = null)
		{
			_publishFactory = publishFactory;
			_exchangeFunc = options?.ExchangeFunc ?? (context => context.GetPublishConfiguration()?.Exchange.ExchangeName);
			_routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetPublishConfiguration()?.RoutingKey);
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var config = ExtractConfigFromMessageType(context) ?? ExtractConfigFromStrings(context);
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config));
			}

			var action = context.Get<Action<IPublishConfigurationBuilder>>(PipeKey.ConfigurationAction);
			if (action != null)
			{
				var builder = new PublishConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.Add(PipeKey.PublishConfiguration, config);
			return Next.InvokeAsync(context);
		}

		protected virtual PublishConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = _routingKeyFunc(context);
			var exchange = _exchangeFunc(context);
			return _publishFactory.Create(exchange, routingKey);
		}

		protected virtual PublishConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = _messageTypeFunc(context);
			return messageType == null
				? null
				: _publishFactory.Create(messageType);
		}
	}
}
