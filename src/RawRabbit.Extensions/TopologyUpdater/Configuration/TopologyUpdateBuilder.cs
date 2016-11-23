using System;
using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Configuration
{
    public class TopologyUpdateBuilder : ITopologySelector, IExchangeUpdateBuilder
    {
        private readonly INamingConventions _conventions;
        private readonly RawRabbitConfiguration _config;
        public List<ExchangeUpdateConfiguration> Exchanges { get; set; }
        private ExchangeUpdateConfiguration _current;

        public TopologyUpdateBuilder(INamingConventions conventions, RawRabbitConfiguration config)
        {
            _conventions = conventions;
            _config = config;
            Exchanges = new List<ExchangeUpdateConfiguration>();
        }

        #region ITopologySelector
        public IExchangeUpdateBuilder ForExchange(string name)
        {
            _current = new ExchangeUpdateConfiguration { ExchangeName = name };
            return this;
        }

        public IExchangeUpdateBuilder ExchangeForMessage<TMessage>()
        {
            _current = new ExchangeUpdateConfiguration
            {
                ExchangeName = _conventions.ExchangeNamingConvention(typeof(TMessage))
            };
            return this;
        }

        public ITopologySelector UseConventionForExchange<TMessage>()
        {
            UseConventionForExchange(typeof(TMessage));
            return this;
        }

        public ITopologySelector UseConventionForExchange(params Type[] messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                Exchanges.Add(new ExchangeUpdateConfiguration(_config.Exchange)
                {
                    ExchangeName = _conventions.ExchangeNamingConvention(messageType)
                });
            }
            return this;
        }
        #endregion

        #region IExchangeUpdateBuilder
        public ITopologySelector UseConfiguration(Action<IExchangeConfigurationBuilder> cfgAction, Func<string, string> bindingKeyTransformer = null)
        {
            var builder = new ExchangeConfigurationBuilder(new ExchangeUpdateConfiguration(_config.Exchange));
            cfgAction(builder);
            var cfg = builder.Configuration as ExchangeUpdateConfiguration;
            cfg.ExchangeName = _current.ExchangeName;
            if (bindingKeyTransformer != null)
            {
                cfg.BindingTransformer = bindingKeyTransformer;
            }
            Exchanges.Add(cfg);
            return this;
        }

        public ITopologySelector UseConfiguration(ExchangeUpdateConfiguration configuration)
        {
            configuration.ExchangeName = _current.ExchangeName;
            Exchanges.Add(configuration);
            return this;
        }

        public ITopologySelector UseConventions<TMessage>(Func<string, string> bindingKeyTransformer = null)
        {
            _current.ExchangeName = _conventions.ExchangeNamingConvention(typeof(TMessage));
            if (bindingKeyTransformer != null)
            {
                _current.BindingTransformer = bindingKeyTransformer;
            }
            Exchanges.Add(_current);
            return this;
        }
        #endregion
    }
}
