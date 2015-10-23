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

		public ConfigurationEvaluator(RawRabbitConfiguration clientConfig, INamingConvetions convetions)
		{
			_clientConfig = clientConfig;
			_convetions = convetions;
		}

		public SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var queueConfig = new QueueConfiguration
			{
				QueueName = _convetions.QueueNamingConvention(typeof(TMessage)),
				AutoDelete = _clientConfig.Queue.AutoDelete,
				Durable = _clientConfig.Queue.Durable,
				Exclusive = _clientConfig.Queue.Exclusive
			};

			var exchangeConfig = new ExchangeConfiguration
			{
				ExchangeName = _convetions.ExchangeNamingConvention(typeof(TMessage)),
				Durable = _clientConfig.Exchange.Durable,
				AutoDelete = _clientConfig.Exchange.AutoDelete,
				ExchangeType = _clientConfig.Exchange.Type.ToString().ToLower(),
			};

			var builder = new SubscriptionConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration)
		{
			var queueConfig = new QueueConfiguration()
			{
				QueueName = _convetions.QueueNamingConvention(typeof(TMessage)),
				AutoDelete = _clientConfig.Queue.AutoDelete,
				Durable = _clientConfig.Queue.Durable,
				Exclusive = _clientConfig.Queue.Exclusive
			};

			var exchangeConfig = new ExchangeConfiguration
			{
				ExchangeName = _convetions.ExchangeNamingConvention(typeof(TMessage)),
				Durable = _clientConfig.Exchange.Durable,
				AutoDelete = _clientConfig.Exchange.AutoDelete,
				ExchangeType = _clientConfig.Exchange.Type.ToString().ToLower(),
			};

			var builder = new PublishConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration)
		{
			var queueConfig = new QueueConfiguration
			{
				QueueName = _convetions.QueueNamingConvention(typeof(TResponse)),
				AutoDelete = _clientConfig.Queue.AutoDelete,
				Durable = _clientConfig.Queue.Durable,
				Exclusive = _clientConfig.Queue.Exclusive
			};

			var exchangeConfig = new ExchangeConfiguration
			{
				ExchangeName = _convetions.RpcExchangeNamingConvention(typeof(TRequest), typeof(TResponse)),
				Durable = _clientConfig.Exchange.Durable,
				AutoDelete = _clientConfig.Exchange.AutoDelete,
				ExchangeType = _clientConfig.Exchange.Type.ToString().ToLower(),
			};

			var builder = new ResponderConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
		{
			var replyQueueConfig = new QueueConfiguration
			{
				QueueName = _convetions.RpcReturnQueueNamingConvention(),
				AutoDelete = true,
				Durable = false,
				Exclusive = true
			};

			var exchangeConfig = new ExchangeConfiguration
			{
				ExchangeName = _convetions.RpcExchangeNamingConvention(typeof(TRequest), typeof(TResponse)),
				Durable = _clientConfig.Exchange.Durable,
				AutoDelete = _clientConfig.Exchange.AutoDelete,
				ExchangeType = _clientConfig.Exchange.Type.ToString().ToLower(),
			};

			var defaultConfig = new RequestConfiguration
			{
				ReplyQueue = replyQueueConfig,
				Exchange = exchangeConfig,
				RoutingKey = _convetions.QueueNamingConvention(typeof(TResponse)),
				ReplyQueueRoutingKey = replyQueueConfig.QueueName
			};

			var builder = new RequestConfigurationBuilder(defaultConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
