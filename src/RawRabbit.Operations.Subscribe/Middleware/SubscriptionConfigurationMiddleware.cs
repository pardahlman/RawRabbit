using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscriptionConfigurationOptions
	{
		public Func<IPipeContext, ConsumerConfiguration> ConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
		public Func<IPipeContext, Action<IConsumerConfigurationBuilder>> ConfigActionFunc { get; internal set; }
	}

	public class SubscriptionConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ILog _logger = LogProvider.For<SubscriptionConfigurationMiddleware>();

		protected readonly IConsumerConfigurationFactory ConfigFactory;
		protected Func<IPipeContext, Type> MessageTypeFunc;
		protected Func<IPipeContext, ConsumerConfiguration> ConfigurationFunc;
		protected Func<IPipeContext, Action<IConsumerConfigurationBuilder>> ConfigActionFunc;

		public SubscriptionConfigurationMiddleware(IConsumerConfigurationFactory configFactory, SubscriptionConfigurationOptions options = null)
		{
			ConfigFactory = configFactory;
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType()) ;
			ConfigurationFunc = options?.ConfigFunc;
			ConfigActionFunc = options?.ConfigActionFunc ?? (context => context.Get<Action<IConsumerConfigurationBuilder>>(PipeKey.ConfigurationAction));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var config = ExtractFromContextProperty(context) ?? ExtractConfigFromMessageType(context);
			if (config == null)
			{
				_logger.Info("Unable to extract configuration for Subscriber.");
			}

			var action = GetConfigurationAction(context);
			if (action != null)
			{
				_logger.Info("Configuration action for {queueName} found.", config?.Consume.QueueName);
				var builder = new ConsumerConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.TryAdd(PipeKey.ConsumerConfiguration, config);
			context.Properties.TryAdd(PipeKey.ConsumeConfiguration, config.Consume);
			context.Properties.TryAdd(PipeKey.QueueDeclaration, config.Queue);
			context.Properties.TryAdd(PipeKey.ExchangeDeclaration, config.Exchange);

			await Next.InvokeAsync(context, token);
		}

		protected virtual ConsumerConfiguration ExtractFromContextProperty(IPipeContext context)
		{
			return ConfigurationFunc?.Invoke(context);
		}

		protected virtual ConsumerConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = MessageTypeFunc(context);
			if (messageType != null)
			{
				_logger.Debug("Found message type {messageType} in context. Creating consumer config based on it.", messageType.Name);
			}
			return messageType == null
				? null
				: ConfigFactory.Create(messageType);
		}

		protected Action<IConsumerConfigurationBuilder> GetConfigurationAction(IPipeContext context)
		{
			return ConfigActionFunc?.Invoke(context);
		}
	}
}
