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
		public List<ExchangeUpdateDeclaration> Exchanges { get; set; }
		private ExchangeUpdateDeclaration _current;

		public TopologyUpdateBuilder(INamingConventions conventions, RawRabbitConfiguration config)
		{
			_conventions = conventions;
			_config = config;
			Exchanges = new List<ExchangeUpdateDeclaration>();
		}

		#region ITopologySelector
		public IExchangeUpdateBuilder ForExchange(string name)
		{
			_current = new ExchangeUpdateDeclaration { Name = name };
			return this;
		}

		public IExchangeUpdateBuilder ExchangeForMessage<TMessage>()
		{
			_current = new ExchangeUpdateDeclaration
			{
				Name = _conventions.ExchangeNamingConvention(typeof(TMessage))
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
				Exchanges.Add(new ExchangeUpdateDeclaration(_config.Exchange)
				{
					Name = _conventions.ExchangeNamingConvention(messageType)
				});
			}
			return this;
		}
		#endregion

		#region IExchangeUpdateBuilder
		public ITopologySelector UseConfiguration(Action<IExchangeDeclarationBuilder> cfgAction, Func<string, string> bindingKeyTransformer = null)
		{
			var builder = new ExchangeDeclarationBuilder(new ExchangeUpdateDeclaration(_config.Exchange));
			cfgAction(builder);
			var cfg = builder.Declaration as ExchangeUpdateDeclaration;
			cfg.Name = _current.Name;
			if (bindingKeyTransformer != null)
			{
				cfg.BindingTransformer = bindingKeyTransformer;
			}
			Exchanges.Add(cfg);
			return this;
		}

		public ITopologySelector UseConfiguration(ExchangeUpdateDeclaration declaration)
		{
			declaration.Name = _current.Name;
			Exchanges.Add(declaration);
			return this;
		}

		public ITopologySelector UseConventions<TMessage>(Func<string, string> bindingKeyTransformer = null)
		{
			_current.Name = _conventions.ExchangeNamingConvention(typeof(TMessage));
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
