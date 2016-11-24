using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumeConfigurationOptions
	{
		public Func<IPipeContext, string> QueueFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }

		public static ConsumeConfigurationOptions For(string queueName, string exchangeName, string routingKey)
		{
			return new ConsumeConfigurationOptions
			{
				ExchangeFunc = context => exchangeName,
				QueueFunc = context => queueName,
				RoutingKeyFunc = context => routingKey
			};
		}

		public static ConsumeConfigurationOptions For<TMessage>()
		{
			return new ConsumeConfigurationOptions
			{
				MessageTypeFunc = context => typeof(TMessage)
			};
		}
	}

	public class ConsumeConfigurationMiddleware : Middleware
	{
		private readonly IConsumerConfigurationFactory _configFactory;
		private readonly Func<IPipeContext, string> _queueFunc;
		private readonly Func<IPipeContext, string> _exchangeFunc;
		private readonly Func<IPipeContext, string> _routingKeyFunc;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;

		public ConsumeConfigurationMiddleware(IConsumerConfigurationFactory configFactory, ConsumeConfigurationOptions options = null)
		{
			_configFactory = configFactory;
			_queueFunc = options?.QueueFunc ?? (context => context.GetQueueConfiguration()?.QueueName);
			_exchangeFunc = options?.ExchangeFunc ?? (context => context.GetExchangeConfiguration()?.ExchangeName);
			_routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetRoutingKey());
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var config = ExtractConfigFromMessageType(context) ?? ExtractConfigFromStrings(context) ?? CreateDefaultConfig(context);

			var action = context.Get<Action<IConsumerConfigurationBuilder>>(PipeKey.ConfigurationAction);
			if (action != null)
			{
				var builder = new ConsumerConfigurationBuilder(config);
				action(builder);
				config = builder.Config;
			}

			context.Properties.Add(PipeKey.ConsumerConfiguration, config);

			return Next.InvokeAsync(context);
		}

		protected  virtual ConsumerConfiguration CreateDefaultConfig(IPipeContext context)
		{
			var clientCfg = context.GetClientConfiguration();
			return new ConsumerConfiguration
			{
				Queue = new QueueConfiguration(clientCfg.Queue),
				Exchange = new ExchangeConfiguration(clientCfg.Exchange),
			};
		}

		protected virtual ConsumerConfiguration ExtractConfigFromStrings(IPipeContext context)
		{
			var routingKey = _routingKeyFunc(context);
			var queueName = _queueFunc(context);
			var exchangeName = _exchangeFunc(context);
			return _configFactory.Create(queueName, exchangeName, routingKey);
		}

		protected virtual ConsumerConfiguration ExtractConfigFromMessageType(IPipeContext context)
		{
			var messageType = _messageTypeFunc(context);
			return messageType == null
				? null
				: _configFactory.Create(messageType);
		}
	}
}
