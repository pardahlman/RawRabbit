using System;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.TopologyUpdater.Configuration;
using RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater
{
	public static class TopologyUpdaterExtension
	{
		public static Task<TopologyUpdateResult> UpdateTopologyAsync<TMessageContext>(this ILegacyBusClient<TMessageContext> client, Action<ITopologySelector> config) where TMessageContext : IMessageContext
		{
			var extended = (client as Client.ILegacyBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Extensions is only available for ExtendableBusClient");
			}

			var conventions = extended.GetService<INamingConventions>();
			var clientConfig = extended.GetService<RawRabbitConfiguration>();
			var exchangeUpdater = extended.GetService<IExchangeUpdater>();

			var configBuilder = new TopologyUpdateBuilder(conventions, clientConfig);
			config(configBuilder);
			var exchangeConfig = configBuilder.Exchanges
				.GroupBy(x => x.ExchangeName)
				.Select(g => g.LastOrDefault());

			var exchangesTask =  exchangeUpdater.UpdateExchangesAsync(exchangeConfig);

			return Task
				.WhenAll(exchangesTask)
				.ContinueWith(t => new TopologyUpdateResult
				{
					Exchanges = exchangesTask.Result.ToList()
				});
		}
	}
}
