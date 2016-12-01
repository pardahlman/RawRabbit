using System;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction
{
	public interface IExchangeUpdateBuilder
	{
		ITopologySelector UseConfiguration(Action<IExchangeDeclarationBuilder> cfgAction, Func<string, string> bindingKeyTransformer = null);
		ITopologySelector UseConfiguration(ExchangeUpdateDeclaration declaration);
		ITopologySelector UseConventions<TMessage>(Func<string, string> bindingKeyTransformer = null);
	}
}