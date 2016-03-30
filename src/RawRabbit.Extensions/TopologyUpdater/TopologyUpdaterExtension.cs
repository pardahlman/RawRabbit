using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.TopologyUpdater.Configuration;
using RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;

namespace RawRabbit.Extensions.TopologyUpdater
{
	public static class TopologyUpdaterExtension
	{
		public static Task UpdateTopologyAsync<TMessageContext>(this IBusClient<TMessageContext> client, Action<ITopologySelector> config) where TMessageContext : IMessageContext
		{
			var extended = (client as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Extensions is only available for ExtendableBusClient");
			}

			var conventions = extended.GetService<INamingConventions>();
			var clientConfig = extended.GetService<RawRabbitConfiguration>();
			var exchangeUpdater = extended.GetService<IExchangeUpdater>();

			var configBuilder = new TopologyUpdateBuilder(conventions, clientConfig);
			config(configBuilder);

			return exchangeUpdater.UpdateExchangesAsync(configBuilder.Exchanges);
		}
	}
}
