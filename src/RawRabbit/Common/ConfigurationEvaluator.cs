using System;
using RawRabbit.Common.Conventions;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null) where T : MessageBase;
		PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration) where T : MessageBase;
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
	}
}
