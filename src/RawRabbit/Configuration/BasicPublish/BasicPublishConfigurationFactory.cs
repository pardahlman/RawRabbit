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
				throw new ArgumentNullException(nameof(message));
			}
			return new BasicPublishConfiguration
			{
				RoutingKey = GetRoutingKey(message),
				BasicProperties = GetBasicProperties(message),
				Body = GetBody(message),
				ExchangeName = GetExchangeName(message),
				Mandatory = GetMandatory(message)
			};
		}

		public virtual BasicPublishConfiguration Create()
		{
			return new BasicPublishConfiguration
			{
				BasicProperties = _provider.GetProperties()
			};
		}

		protected  virtual string GetRoutingKey(object message)
		{
			return _conventions.RoutingKeyConvention(message.GetType());
		}

		protected virtual bool GetMandatory(object message)
		{
			return false;
		}

		protected virtual string GetExchangeName(object message)
		{
			return _conventions.ExchangeNamingConvention(message.GetType());
		}

		protected virtual IBasicProperties GetBasicProperties(object message)
		{
			return _provider.GetProperties(message.GetType());
		}

		protected virtual byte[] GetBody(object message)
		{
			var serialized = _serializer.Serialize(message);
			return UTF8Encoding.UTF8.GetBytes(serialized);
		}
	}
}