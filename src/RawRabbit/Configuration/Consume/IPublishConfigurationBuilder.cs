using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Consume
{
	public interface IPublishConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IPublishConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublishConfigurationBuilder WithRoutingKey(string routingKey);
		IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublishConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback);
	}

	public class PublishConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public Action<BasicReturnEventArgs> MandatoryCallback { get; set; }
		public Action<IBasicProperties> PropertyModifier { get; set; }
		public string RoutingKey { get; set; }
	}

	public class PublishConfigurationBuilder : IPublishConfigurationBuilder
	{
		public PublishConfiguration Config { get; }

		public PublishConfigurationBuilder(PublishConfiguration initial)
		{
			Config = initial;
		}

		public IPublishConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			var builder = new ExchangeConfigurationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Configuration;
			return this;
		}

		public IPublishConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties)
		{
			Config.PropertyModifier = Config.PropertyModifier ?? (b => { });
			Config.PropertyModifier += properties;
			return this;
		}

		public IPublishConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
		{
			Config.MandatoryCallback = Config.MandatoryCallback ?? (a => { });
			Config.MandatoryCallback += callback;
			return this;
		}
	}

	public interface IPublishConfigurationFactory
	{
		PublishConfiguration Create<TMessage>();
		PublishConfiguration Create(Type messageType);
		PublishConfiguration Create(string exchangeName, string routingKey);
	}

	public class PublishConfigurationFactory : IPublishConfigurationFactory
	{
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly INamingConventions _conventions;

		public PublishConfigurationFactory(IExchangeConfigurationFactory exchange, INamingConventions conventions)
		{
			_exchange = exchange;
			_conventions = conventions;
		}

		public PublishConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public PublishConfiguration Create(Type messageType)
		{
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			var routingKey = _conventions.RoutingKeyConvention(messageType);
			return Create(exchangeName, routingKey);
		}

		public PublishConfiguration Create(string exchangeName, string routingKey)
		{
			return new PublishConfiguration
			{
				Exchange = _exchange.Create(exchangeName),
				RoutingKey = routingKey
			};
		}
	}
}
