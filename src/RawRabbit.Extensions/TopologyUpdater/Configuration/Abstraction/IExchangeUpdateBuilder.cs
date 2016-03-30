using System;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction
{
	public interface IExchangeUpdateBuilder
	{
		ITopologySelector UseConfiguration(Action<IExchangeConfigurationBuilder> cfgAction);
		ITopologySelector UseConfiguration(ExchangeConfiguration configuration);
		ITopologySelector UseConventions<TMessage>();
	}
}