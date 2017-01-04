using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Logging;
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
		protected readonly IPublisherConfigurationFactory PublisherFactory;
		protected readonly Func<IPipeContext, string> ExchangeFunc;
		protected readonly Func<IPipeContext, string> RoutingKeyFunc;
		protected readonly Func<IPipeContext, Type> MessageTypeFunc;
		private readonly ILogger _logger = LogManager.GetLogger<PublisherConfigurationMiddleware>();

		public PublisherConfigurationMiddleware(IPublisherConfigurationFactory publisherFactory, PublishConfigurationOptions options = null)
		{
			PublisherFactory = publisherFactory;
			ExchangeFunc = options?.ExchangeFunc ?? (context => context.GetPublishConfiguration()?.Exchange.Name);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetPublishConfiguration()?.RoutingKey);
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var config = ExtractConfigFromMessageType(context) ?? ExtractConfigFromStrings(context);
			if (config == null)
			{
				_logger.LogWarning("Unable to find PublisherConfiguration from message type or parameters.");
				throw new ArgumentNullException(nameof(config));
			}

			var action = context.Get<Action<IPublisherConfigurationBuilder>>(PipeKey.ConfigurationAction);
			if (action != null)
			{
				_logger.LogDebug($"Custom configuration supplied. Applying.");
				var builder = new PublisherConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.TryAdd(PipeKey.PublisherConfiguration, config);
			context.Properties.TryAdd(PipeKey.BasicPublishConfiguration, config);
			context.Properties.TryAdd(PipeKey.ExchangeDeclaration, config.Exchange);
			context.Properties.TryAdd(PipeKey.ReturnedMessageCallback, config.MandatoryCallback);

			return Next.InvokeAsync(context, token);
		}

		protected virtual PublisherConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = RoutingKeyFunc(context);
			var exchange = ExchangeFunc(context);
			return PublisherFactory.Create(exchange, routingKey);
		}

		protected virtual PublisherConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = MessageTypeFunc(context);
			return messageType == null
				? null
				: PublisherFactory.Create(messageType);
		}
	}
}
