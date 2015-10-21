using System;
using RawRabbit.Common.Conventions;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Request;
using RawRabbit.Core.Configuration.Respond;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null) where T : MessageBase;
		PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration) where T : MessageBase;
		ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration) where TRequest : MessageBase where TResponse : MessageBase;
		RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration) where TRequest : MessageBase where TResponse : MessageBase;
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

		public SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null) where T : MessageBase
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<T>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<T>();

			var builder = new SubscriptionConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration) where T : MessageBase
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<T>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<T>();

			var builder = new PublishConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration) where TRequest : MessageBase where TResponse: MessageBase
		{
			var defaultQueue = _queueConventions.CreateQueueConfiguration<TResponse>();
			var defaultExchange = _exchangeConventions.CreateDefaultConfiguration<TRequest>();
			
			var builder = new ResponderConfigurationBuilder(defaultQueue, defaultExchange);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
			where TRequest : MessageBase
			where TResponse : MessageBase
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
