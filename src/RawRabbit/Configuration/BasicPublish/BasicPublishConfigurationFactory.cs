using System;
using System.Text;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Serialization;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationFactory : IBasicPublishConfigurationFactory
	{
		private readonly INamingConventions _conventions;
		private readonly IBasicPropertiesProvider _provider;
		private readonly ISerializer _serializer;

		public BasicPublishConfigurationFactory(INamingConventions conventions, IBasicPropertiesProvider provider, ISerializer serializer)
		{
			_conventions = conventions;
			_provider = provider;
			_serializer = serializer;
		}

		public virtual BasicPublishConfiguration Create(object message)
		{
			if (message == null)
			{
				return Create();
			}
			var cfg = Create(message.GetType());
			cfg.Body = GetBody(message);
			return cfg;
		}

		public virtual BasicPublishConfiguration Create(Type type)
		{
			return new BasicPublishConfiguration
			{
				RoutingKey = GetRoutingKey(type),
				BasicProperties = GetBasicProperties(type),
				ExchangeName = GetExchangeName(type),
				Mandatory = GetMandatory(type)
			};
		}

		public virtual BasicPublishConfiguration Create()
		{
			return new BasicPublishConfiguration
			{
				BasicProperties = _provider.GetProperties()
			};
		}

		protected  virtual string GetRoutingKey(Type type)
		{
			return _conventions.RoutingKeyConvention(type);
		}

		protected virtual bool GetMandatory(Type type)
		{
			return false;
		}

		protected virtual string GetExchangeName(Type type)
		{
			return _conventions.ExchangeNamingConvention(type);
		}

		protected virtual IBasicProperties GetBasicProperties(Type type)
		{
			return _provider.GetProperties(type);
		}

		protected virtual byte[] GetBody(object message)
		{
			var serialized = _serializer.Serialize(message);
			return UTF8Encoding.UTF8.GetBytes(serialized);
		}
	}
}