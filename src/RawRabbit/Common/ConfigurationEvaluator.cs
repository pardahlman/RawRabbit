using System;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Conventions;

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
		private readonly IQueueConventions _queueConventions;
		private readonly IExchangeConventions _exchangeConventions;

		public ConfigurationEvaluator(IQueueConventions queueConventions, IExchangeConventions exchangeConventions)
		{
			_queueConventions = queueConventions;
			_exchangeConventions = exchangeConventions;
		}

		public SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<T>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<T>();

			var builder = new SubscriptionConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration)
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<T>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<T>();

			var builder = new PublishConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration)
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<TResponse>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<TRequest>();
			
			var builder = new ResponderConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
		{
			var defaultConfig = new RequestConfiguration
			{
				ReplyQueue = _queueConventions.CreateResponseQueueConfiguration<TRequest>(),
				Exchange = _exchangeConventions.CreateDefaultConfiguration<TRequest>(),
				RoutingKey = _queueConventions.CreateQueueConfiguration<TResponse>().QueueName
			};

			var builder = new RequestConfigurationBuilder(defaultConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
