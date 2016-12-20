using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishConfigurationOptions
	{
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class PublisherConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IPublisherConfigurationFactory _publisherFactory;
		private readonly Func<IPipeContext, string> _exchangeFunc;
		private readonly Func<IPipeContext, string> _routingKeyFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public PublisherConfigurationMiddleware(IPublisherConfigurationFactory publisherFactory, PublishConfigurationOptions options = null)
		{
			_publisherFactory = publisherFactory;
			_exchangeFunc = options?.ExchangeFunc ?? (context => context.GetPublishConfiguration()?.Exchange.Name);
			_routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetPublishConfiguration()?.RoutingKey);
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var config = ExtractConfigFromMessageType(context) ?? ExtractConfigFromStrings(context);
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config));
			}

			var action = context.Get<Action<IPublisherConfigurationBuilder>>(PipeKey.ConfigurationAction);
			if (action != null)
			{
				var builder = new PublisherConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.Add(PipeKey.BasicPublishConfiguration, config);
			context.Properties.Add(PipeKey.ExchangeDeclaration, config.Exchange);
			context.Properties.Add(PipeKey.ReturnedMessageCallback, config.MandatoryCallback);

			return Next.InvokeAsync(context, token);
		}

		protected virtual PublisherConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = _routingKeyFunc(context);
			var exchange = _exchangeFunc(context);
			return _publisherFactory.Create(exchange, routingKey);
		}

		protected virtual PublisherConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = _messageTypeFunc(context);
			return messageType == null
				? null
				: _publisherFactory.Create(messageType);
		}
	}
}
