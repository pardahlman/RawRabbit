using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeConfigurationOptions
	{
		public Func<IPipeContext, string> QueueFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
		public Func<IPipeContext, Action<IConsumeConfigurationBuilder>> ConfigActionFunc { get; set; }
	}

	public class ConsumeConfigurationMiddleware : Middleware
	{
		protected IConsumeConfigurationFactory ConfigFactory;
		protected Func<IPipeContext, string> QueueFunc;
		protected Func<IPipeContext, string> ExchangeFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;
		protected Func<IPipeContext, Type> MessageTypeFunc;
		protected Func<IPipeContext, Action<IConsumeConfigurationBuilder>> ConfigActionFunc;
		private readonly ILog _logger = LogProvider.For<ConsumeConfigurationMiddleware>();

		public ConsumeConfigurationMiddleware(IConsumeConfigurationFactory configFactory, ConsumeConfigurationOptions options = null)
		{
			ConfigFactory = configFactory;
			QueueFunc = options?.QueueFunc ?? (context => context.GetQueueDeclaration()?.Name);
			ExchangeFunc = options?.ExchangeFunc ?? (context => context.GetExchangeDeclaration()?.Name);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetRoutingKey());
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
			ConfigActionFunc = options?.ConfigActionFunc ?? (context => context.Get<Action<IConsumeConfigurationBuilder>>(PipeKey.ConfigurationAction));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var config = ExtractConfigFromMessageType(context) ?? ExtractConfigFromStrings(context);

			var action = GetConfigurationAction(context);
			if (action != null)
			{
				_logger.Info("Configuration action for {queueName} found.", config?.QueueName);
				var builder = new ConsumeConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.TryAdd(PipeKey.ConsumeConfiguration, config);

			await Next.InvokeAsync(context, token);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return MessageTypeFunc(context);
		}

		protected Action<IConsumeConfigurationBuilder> GetConfigurationAction(IPipeContext context)
		{
			return ConfigActionFunc(context);
		}

		protected virtual ConsumeConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = RoutingKeyFunc(context);
			var queueName = QueueFunc(context);
			var exchangeName = ExchangeFunc(context);
			_logger.Debug("Consuming from queue {queueName} on {exchangeName} with routing key {routingKey}", queueName, exchangeName, routingKey);
			return ConfigFactory.Create(queueName, exchangeName, routingKey);
		}

		protected virtual ConsumeConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = MessageTypeFunc(context);
			if (messageType != null)
			{
				_logger.Debug("Found message type {messageType} in context. Creating consume config based on it.", messageType.Name);
			}
			return messageType == null
				? null
				: ConfigFactory.Create(messageType);
		}
	}

	public static class BasicConsumeExtensions
	{
		public static IPipeContext UseConsumeConfiguration(this IPipeContext context, Action<IConsumeConfigurationBuilder> config)
		{
			context.Properties.TryAdd(PipeKey.ConfigurationAction, config);
			return context;
		}
	}
}
