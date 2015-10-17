using System;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Subscribe;

namespace RawRabbit.Common
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration);
	}

	public class ConfigurationEvaluator : IConfigurationEvaluator
	{
		public SubscriptionConfiguration GetConfiguration<T>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var builder = new SubscriptionConfigurationBuilder();
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration<T>(Action<IPublishConfigurationBuilder> configuration)
		{
			var builder = new PublishConfigurationBuilder();
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
