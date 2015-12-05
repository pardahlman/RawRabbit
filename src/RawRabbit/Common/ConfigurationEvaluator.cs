using System;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;

namespace RawRabbit.Common
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration);
		ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration);
		RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration);
	}

	public class ConfigurationEvaluator : IConfigurationEvaluator
	{
		private readonly RawRabbitConfiguration _clientConfig;
		private readonly INamingConvetions _convetions;
		private readonly string _directReplyTo = "amq.rabbitmq.reply-to";

		public ConfigurationEvaluator(RawRabbitConfiguration clientConfig, INamingConvetions convetions)
		{
			_clientConfig = clientConfig;
			_convetions = convetions;
		}

		public SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var routingKey = _convetions.QueueNamingConvention(typeof(TMessage));
			var queueConfig = new QueueConfiguration(_clientConfig.Queue)
			{
				QueueName = routingKey,
				NameSuffix = _convetions.SubscriberQueueSuffix(typeof(TMessage))
			};

			var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
			{
				ExchangeName = _convetions.ExchangeNamingConvention(typeof(TMessage))
			};

			var builder = new SubscriptionConfigurationBuilder(queueConfig, exchangeConfig, routingKey);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration)
		{
			var queueConfig = new QueueConfiguration(_clientConfig.Queue)
			{
				QueueName = _convetions.QueueNamingConvention(typeof(TMessage)),
			};

			var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
			{
				ExchangeName = _convetions.ExchangeNamingConvention(typeof(TMessage))
			};

			var builder = new PublishConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration)
		{
			var queueConfig = new QueueConfiguration(_clientConfig.Queue)
			{
				QueueName = _convetions.QueueNamingConvention(typeof(TRequest))
			};

			var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
			{
				ExchangeName = _convetions.RpcExchangeNamingConvention(typeof(TRequest), typeof(TResponse)),
			};

			var builder = new ResponderConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
		{
			// leverage direct reply to: https://www.rabbitmq.com/direct-reply-to.html
			var replyQueueConfig = new QueueConfiguration
			{
				QueueName = _directReplyTo,
				AutoDelete = true,
				Durable = false,
				Exclusive = true
			};

			var exchangeConfig = new ExchangeConfiguration(_clientConfig.Exchange)
			{
				ExchangeName = _convetions.RpcExchangeNamingConvention(typeof(TRequest), typeof(TResponse))
			};

			var defaultConfig = new RequestConfiguration
			{
				ReplyQueue = replyQueueConfig,
				Exchange = exchangeConfig,
				RoutingKey = _convetions.QueueNamingConvention(typeof(TRequest)),
				ReplyQueueRoutingKey = replyQueueConfig.QueueName
			};

			var builder = new RequestConfigurationBuilder(defaultConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
