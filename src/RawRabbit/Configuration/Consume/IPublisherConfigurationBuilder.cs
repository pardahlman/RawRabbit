using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Consume
{
	public interface IPublisherConfigurationBuilder
	{
		/// <summary>
		/// Specify the topology features of the Exchange to consume from.
		/// Exchange will be declared.
		/// </summary>
		/// <param name="exchange">Builder for exchange features.</param>
		/// <returns></returns>
		IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange);
		IPublisherConfigurationBuilder WithRoutingKey(string routingKey);
		IPublisherConfigurationBuilder WithProperties(Action<IBasicProperties> properties);
		IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback);
	}

	public class PublisherConfiguration
	{
		public ExchangeConfiguration Exchange { get; set; }
		public Action<BasicReturnEventArgs> MandatoryCallback { get; set; }
		public Action<IBasicProperties> PropertyModifier { get; set; }
		public string RoutingKey { get; set; }
	}

	public class PublisherConfigurationBuilder : IPublisherConfigurationBuilder
	{
		public PublisherConfiguration Config { get; }

		public PublisherConfigurationBuilder(PublisherConfiguration initial)
		{
			Config = initial;
		}

		public IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			var builder = new ExchangeConfigurationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Configuration;
			return this;
		}

		public IPublisherConfigurationBuilder WithRoutingKey(string routingKey)
		{
			Config.RoutingKey = routingKey;
			return this;
		}

		public IPublisherConfigurationBuilder WithProperties(Action<IBasicProperties> properties)
		{
			Config.PropertyModifier = Config.PropertyModifier ?? (b => { });
			Config.PropertyModifier += properties;
			return this;
		}

		public IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
		{
			Config.MandatoryCallback = Config.MandatoryCallback ?? (a => { });
			Config.MandatoryCallback += callback;
			return this;
		}
	}

	public interface IPublisherConfigurationFactory
	{
		PublisherConfiguration Create<TMessage>();
		PublisherConfiguration Create(Type messageType);
		PublisherConfiguration Create(string exchangeName, string routingKey);
	}

	public class PublisherConfigurationFactory : IPublisherConfigurationFactory
	{
		private readonly IExchangeConfigurationFactory _exchange;
		private readonly INamingConventions _conventions;

		public PublisherConfigurationFactory(IExchangeConfigurationFactory exchange, INamingConventions conventions)
		{
			_exchange = exchange;
			_conventions = conventions;
		}

		public PublisherConfiguration Create<TMessage>()
		{
			return Create(typeof(TMessage));
		}

		public PublisherConfiguration Create(Type messageType)
		{
			var exchangeName = _conventions.ExchangeNamingConvention(messageType);
			var routingKey = _conventions.RoutingKeyConvention(messageType);
			return Create(exchangeName, routingKey);
		}

		public PublisherConfiguration Create(string exchangeName, string routingKey)
		{
			return new PublisherConfiguration
			{
				Exchange = _exchange.Create(exchangeName),
				RoutingKey = routingKey
			};
		}
	}
}
