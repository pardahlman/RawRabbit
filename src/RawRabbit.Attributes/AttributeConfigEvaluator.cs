using System;
using System.Reflection;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;

namespace RawRabbit.Attributes
{
    public class AttributeConfigEvaluator : IConfigurationEvaluator
    {
        private readonly ConfigurationEvaluator _fallback;

        public AttributeConfigEvaluator(RawRabbitConfiguration config, INamingConventions conventions)
        {
            _fallback = new ConfigurationEvaluator(config, conventions);
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
            configuration = configuration ?? (builder => { });
            configuration = (builder =>
            {
                builder
                    .WithExchange(ExchangeAction(messageType))
                    .WithQueue(QueueAction(messageType));

                var routingAttr = GetAttribute<RoutingAttribute>(messageType);
                if (routingAttr != null)
                {
                    if (routingAttr.NullableNoAck.HasValue)
                    {
                        builder.WithNoAck(routingAttr.NullableNoAck.Value);
                    }
                    if (routingAttr.PrefetchCount > 0)
                    {
                        builder.WithPrefetchCount(routingAttr.PrefetchCount);
                    }
                    if (routingAttr.RoutingKey != null)
                    {
                        builder.WithRoutingKey(routingAttr.RoutingKey);
                    }
                }
            }) + configuration;
            var cfg = _fallback.GetConfiguration(messageType, configuration);
            return cfg;
        }

        public PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration)
        {
            configuration = configuration ?? (builder => { });
            configuration = (builder =>
            {
                builder.WithExchange(ExchangeAction(messageType));
                var routingAttr = GetAttribute<RoutingAttribute>(messageType);
                if (routingAttr?.RoutingKey != null)
                {
                    builder.WithRoutingKey(routingAttr.RoutingKey);
                }
            }) + configuration;
            var cfg = _fallback.GetConfiguration(messageType, configuration);
            return cfg;
        }

        public ResponderConfiguration GetConfiguration(Type requestType, Type responseType, Action<IResponderConfigurationBuilder> configuration)
        {
            configuration = configuration ?? (builder => { });
            configuration = (builder =>
            {
                builder
                    .WithExchange(ExchangeAction(requestType))
                    .WithQueue(QueueAction(requestType));

                var routingAttr = GetAttribute<RoutingAttribute>(requestType);
                if (routingAttr != null)
                {
                    if (routingAttr.NullableNoAck.HasValue)
                    {
                        builder.WithNoAck(routingAttr.NullableNoAck.Value);
                    }
                    if (routingAttr.PrefetchCount > 0)
                    {
                        builder.WithPrefetchCount(routingAttr.PrefetchCount);
                    }
                    if (routingAttr.RoutingKey != null)
                    {
                        builder.WithRoutingKey(routingAttr.RoutingKey);
                    }
                }
            }) + configuration;
            var cfg = _fallback.GetConfiguration(requestType, responseType, configuration);
            return cfg;
        }

        public RequestConfiguration GetConfiguration(Type requestType, Type responseType, Action<IRequestConfigurationBuilder> configuration)
        {
            configuration = configuration ?? (builder => { });
            configuration = (builder =>
            {
                builder.WithExchange(ExchangeAction(requestType));

                var routingAttr = GetAttribute<RoutingAttribute>(requestType);
                if (routingAttr != null)
                {
                    if (routingAttr.NullableNoAck.HasValue)
                    {
                        builder.WithNoAck(routingAttr.NullableNoAck.Value);
                    }
                    if (routingAttr.RoutingKey != null)
                    {
                        builder.WithRoutingKey(routingAttr.RoutingKey);
                    }
                }
            }) + configuration;
            var cfg = _fallback.GetConfiguration(requestType, responseType, configuration);
            return cfg;
        }

        private static Action<IExchangeConfigurationBuilder> ExchangeAction(Type messageType)
        {
            var exchangeAttr = GetAttribute<ExchangeAttribute>(messageType);
            if (exchangeAttr == null)
            {
                return builder => { };
            }
            return builder =>
            {
                if (!string.IsNullOrWhiteSpace(exchangeAttr.Name))
                {
                    builder.WithName(exchangeAttr.Name);
                }
                if (exchangeAttr.NullableDurability.HasValue)
                {
                    builder.WithDurability(exchangeAttr.NullableDurability.Value);
                }
                if (exchangeAttr.NullableAutoDelete.HasValue)
                {
                    builder.WithDurability(exchangeAttr.NullableAutoDelete.Value);
                }
                if (exchangeAttr.Type != ExchangeType.Unknown)
                {
                    builder.WithType(exchangeAttr.Type);
                }
            };
        }

        private static Action<IQueueConfigurationBuilder> QueueAction(Type messageType)
        {
            var queueAttr = GetAttribute<QueueAttribute>(messageType);
            if (queueAttr == null)
            {
                return builder => { };
            }
            return builder =>
            {
                if (!string.IsNullOrWhiteSpace(queueAttr.Name))
                {
                    builder.WithName(queueAttr.Name);
                }
                if (queueAttr.NullableDurability.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableDurability.Value);
                }
                if (queueAttr.NullableExclusitivy.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableExclusitivy.Value);
                }
                if (queueAttr.NullableAutoDelete.HasValue)
                {
                    builder.WithDurability(queueAttr.NullableAutoDelete.Value);
                }
                if (queueAttr.MessageTtl > 0)
                {
                    builder.WithArgument(QueueArgument.MessageTtl, queueAttr.MessageTtl);
                }
                if (queueAttr.MaxPriority > 0)
                {
                    builder.WithArgument(QueueArgument.MaxPriority, queueAttr.MaxPriority);
                }
                if (!string.IsNullOrWhiteSpace(queueAttr.DeadLeterExchange))
                {
                    builder.WithArgument(QueueArgument.DeadLetterExchange, queueAttr.DeadLeterExchange);
                }
                if (!string.IsNullOrWhiteSpace(queueAttr.Mode))
                {
                    builder.WithArgument(QueueArgument.QueueMode, queueAttr.Mode);
                }
            };
        }

        private static TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
            return attr;
        }
    }
}
