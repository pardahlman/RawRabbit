using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core.Abstraction
{
	public interface IExchangeUpdater
	{
		Task<ExchangeUpdateResult> UpdateExchangeAsync(ExchangeUpdateConfiguration config);
		Task<IEnumerable<ExchangeUpdateResult>> UpdateExchangesAsync(IEnumerable<ExchangeUpdateConfiguration> configs);
	}
}