using System;
using System.Collections.Generic;
using System.Reflection;
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
            properties.Headers.Add(PropertyHeaders.MessageType, GetTypeName(typeof(TMessage)));
            return properties;
        }

        private string GetTypeName(Type type)
        {
            var name = $"{type.Namespace}.{type.Name}";
            if (type.GenericTypeArguments.Length > 0)
            {
                var shouldInsertComma = false;
                name += '[';
                foreach (var genericType in type.GenericTypeArguments)
                {
                    if (shouldInsertComma)
                        name += ",";
                    name += $"[{GetTypeName(genericType)}]";
                    shouldInsertComma = true;
                }
                name += ']';
            }
            name += $", {type.GetTypeInfo().Assembly.GetName().Name}";
            return name;
        }
    }
}
