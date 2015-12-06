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
				Persistent = _config.PersistentDeliveryMode,
				Headers = new Dictionary<string, object>
				{
					{ PropertyHeaders.Sent, DateTime.UtcNow.ToString("u") },
					{ PropertyHeaders.MessageType, typeof(TMessage).FullName }
				}
			};
			custom?.Invoke(properties);
			return properties;
		}
	}
}
