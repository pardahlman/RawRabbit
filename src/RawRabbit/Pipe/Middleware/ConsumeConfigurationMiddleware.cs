using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeConfigurationOptions
	{
		public Func<IPipeContext, string> QueueFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
		public Func<IPipeContext, Action<IConsumerConfigurationBuilder>> ConfigActionFunc { get; set; }
	}

	public class ConsumeConfigurationMiddleware : Middleware
	{
		protected IConsumerConfigurationFactory ConfigFactory;
		protected Func<IPipeContext, string> QueueFunc;
		protected Func<IPipeContext, string> ExchangeFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;
		protected Func<IPipeContext, Type> MessageTypeFunc;
		protected Func<IPipeContext, Action<IConsumerConfigurationBuilder>> ConfigActionFunc;
		private readonly ILogger _logger = LogManager.GetLogger<ConsumeConfigurationMiddleware>();

		public ConsumeConfigurationMiddleware(IConsumerConfigurationFactory configFactory, ConsumeConfigurationOptions options = null)
		{
			ConfigFactory = configFactory;
			QueueFunc = options?.QueueFunc ?? (context => context.GetQueueDeclaration()?.Name);
			ExchangeFunc = options?.ExchangeFunc ?? (context => context.GetExchangeDeclaration()?.Name);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetRoutingKey());
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
			ConfigActionFunc = options?.ConfigActionFunc ?? (context => context.Get<Action<IConsumerConfigurationBuilder>>(PipeKey.ConfigurationAction));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var config = ExtractConfigFromMessageType(context)
				?? ExtractConfigFromStrings(context)
				?? CreateDefaultConfig(context);

			var action = GetConfigurationAction(context);
			if (action != null)
			{
				_logger.LogInformation($"Configuration action for '{config.Queue?.Name}' found.");
				var builder = new ConsumerConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.TryAdd(PipeKey.ConsumerConfiguration, config);
			context.Properties.TryAdd(PipeKey.ConsumeConfiguration, config.Consume);
			context.Properties.TryAdd(PipeKey.QueueDeclaration, config.Queue);
			context.Properties.TryAdd(PipeKey.ExchangeDeclaration, config.Exchange);

			return Next.InvokeAsync(context, token);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return MessageTypeFunc(context);
		}

		protected Action<IConsumerConfigurationBuilder> GetConfigurationAction(IPipeContext context)
		{
			return ConfigActionFunc(context);
		}

		protected  virtual ConsumerConfiguration CreateDefaultConfig(IPipeContext context)
		{
			var clientCfg = context.GetClientConfiguration();
			return new ConsumerConfiguration
			{
				Queue = new QueueDeclaration(clientCfg.Queue),
				Exchange = new ExchangeDeclaration(clientCfg.Exchange),
			};
		}

		protected virtual ConsumerConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = RoutingKeyFunc(context);
			var queueName = QueueFunc(context);
			var exchangeName = ExchangeFunc(context);
			_logger.LogDebug($"Consuming from queue '{queueName}' on '{exchangeName}' with routing key '{routingKey}'");
			return ConfigFactory.Create(queueName, exchangeName, routingKey);
		}

		protected virtual ConsumerConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = MessageTypeFunc(context);
			if (messageType != null)
			{
				_logger.LogDebug($"Found message type '{messageType.Name}' in context. Creating consume config based on it.");
			}
			return messageType == null
				? null
				: ConfigFactory.Create(messageType);
		}
	}
}
