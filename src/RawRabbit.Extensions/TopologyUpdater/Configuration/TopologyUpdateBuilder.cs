using System;
using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction;

namespace RawRabbit.Extensions.TopologyUpdater.Configuration
{
	public class TopologyUpdateBuilder : ITopologySelector, IExchangeUpdateBuilder
	{
		private readonly INamingConventions _conventions;
		private readonly RawRabbitConfiguration _config;
		public List<ExchangeConfiguration> Exchanges { get; set; }
		private ExchangeConfiguration _current;

		public TopologyUpdateBuilder(INamingConventions conventions, RawRabbitConfiguration config)
		{
			_conventions = conventions;
			_config = config;
			Exchanges = new List<ExchangeConfiguration>();
		}

		#region ITopologySelector
		public IExchangeUpdateBuilder ForExchange(string name)
		{
			_current = new ExchangeConfiguration { ExchangeName = name };
			return this;
		}

		public IExchangeUpdateBuilder ExchangeForMessage<TMessage>()
		{
			_current = new ExchangeConfiguration
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
				Exchanges.Add(new ExchangeConfiguration(_config.Exchange)
				{
					ExchangeName = _conventions.ExchangeNamingConvention(messageType)
				});
			}
			return this;
		}
		#endregion

		#region IExchangeUpdateBuilder
		public ITopologySelector UseConfiguration(Action<IExchangeConfigurationBuilder> cfgAction)
		{
			var builder = new ExchangeConfigurationBuilder(new ExchangeConfiguration(_config.Exchange));
			cfgAction(builder);
			var cfg = builder.Configuration;
			cfg.ExchangeName = _current.ExchangeName;
			Exchanges.Add(cfg);
			return this;
		}

		public ITopologySelector UseConfiguration(ExchangeConfiguration configuration)
		{
			configuration.ExchangeName = _current.ExchangeName;
			Exchanges.Add(configuration);
			return this;
		}

		public ITopologySelector UseConventions<TMessage>()
		{
			_current.ExchangeName = _conventions.ExchangeNamingConvention(typeof(TMessage));
			Exchanges.Add(_current);
			return this;
		}
		#endregion
	}
}
