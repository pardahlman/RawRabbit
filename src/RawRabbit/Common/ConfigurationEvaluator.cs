using System;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Legacy.Publish;
using RawRabbit.Configuration.Legacy.Request;
using RawRabbit.Configuration.Legacy.Respond;
using RawRabbit.Configuration.Legacy.Subscribe;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Common
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration);
		ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration);
		RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration);

		SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration);
		ResponderConfiguration GetConfiguration(Type requestType, Type responseType, Action<IResponderConfigurationBuilder> configuration);
		RequestConfiguration GetConfiguration(Type requestType, Type responseType, Action<IRequestConfigurationBuilder> configuration);
	}

	public class ConfigurationEvaluator : IConfigurationEvaluator
	{
		private readonly RawRabbitConfiguration _clientConfig;
		private readonly INamingConventions _conventions;
		private readonly string _directReplyTo = "amq.rabbitmq.reply-to";

		public ConfigurationEvaluator(RawRabbitConfiguration clientConfig, INamingConventions conventions)
		{
			_clientConfig = clientConfig;
			_conventions = conventions;
		}

		public SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return GetConfiguration(typeof(TMessage), configuration);
		}

		public PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration)
		{
			return GetConfiguration(typeof(TMessage), configuration);
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration)
		{
			return GetConfiguration(typeof(TRequest), typeof(TResponse), configuration);
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
		{
			return GetConfiguration(typeof(TRequest), typeof(TResponse), configuration);
		}

		public SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var routingKey = _conventions.QueueNamingConvention(messageType);
			var queueConfig = new QueueDeclaration(_clientConfig.Queue)
			{
				Name = routingKey,
				NameSuffix = _conventions.SubscriberQueueSuffix(messageType)
			};

			var exchangeConfig = new ExchangeDeclaration(_clientConfig.Exchange)
			{
				Name = _conventions.ExchangeNamingConvention(messageType)
			};

			var builder = new SubscriptionConfigurationBuilder(queueConfig, exchangeConfig, routingKey);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration)
		{
			var exchangeConfig = new ExchangeDeclaration(_clientConfig.Exchange)
			{
				Name = _conventions.ExchangeNamingConvention(messageType)
			};
			var routingKey = _conventions.QueueNamingConvention(messageType);
			var builder = new PublishConfigurationBuilder(exchangeConfig, routingKey);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration(Type requestType, Type responseType, Action<IResponderConfigurationBuilder> configuration)
		{
			var queueConfig = new QueueDeclaration(_clientConfig.Queue)
			{
				Name = _conventions.QueueNamingConvention(requestType)
			};

			var exchangeConfig = new ExchangeDeclaration(_clientConfig.Exchange)
			{
				Name = _conventions.ExchangeNamingConvention(requestType)
			};

			var builder = new ResponderConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration(Type requestType, Type responseType, Action<IRequestConfigurationBuilder> configuration)
		{
			// leverage direct reply to: https://www.rabbitmq.com/direct-reply-to.html
			var replyQueueConfig = new QueueDeclaration
			{
				Name = _directReplyTo,
				AutoDelete = true,
				Durable = false,
				Exclusive = true
			};

			var exchangeConfig = new ExchangeDeclaration(_clientConfig.Exchange)
			{
				Name = _conventions.ExchangeNamingConvention(requestType)
			};

			var defaultConfig = new RequestConfiguration
			{
				ReplyQueue = replyQueueConfig,
				Exchange = exchangeConfig,
				RoutingKey = _conventions.QueueNamingConvention(requestType),
				ReplyQueueRoutingKey = replyQueueConfig.Name
			};

			var builder = new RequestConfigurationBuilder(defaultConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
