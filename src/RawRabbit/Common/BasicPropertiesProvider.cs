using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public interface IBasicPropertiesProvider
	{
		IBasicProperties GetProperties<TMessage>(Action<IBasicProperties> custom = null);
	}

	public class BasicPropertiesProvider : IBasicPropertiesProvider
	{
		private readonly RawRabbitConfiguration _config;

		public BasicPropertiesProvider(RawRabbitConfiguration config)
		{
			_config = config;
		}

		public IBasicProperties GetProperties<TMessage>(Action<IBasicProperties> custom = null)
		{
			var properties = new BasicProperties
			{
				MessageId = Guid.NewGuid().ToString(),
				Headers = new Dictionary<string, object>(),
				Persistent = _config.PersistentDeliveryMode
			};
			custom?.Invoke(properties);
			properties.Headers.Add(PropertyHeaders.Sent, DateTime.UtcNow.ToString("u"));
			properties.Headers.Add(PropertyHeaders.MessageType, typeof(TMessage).FullName);

			return properties;
		}
	}
}
