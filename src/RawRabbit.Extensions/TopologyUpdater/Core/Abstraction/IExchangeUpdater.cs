using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core.Abstraction
{
	public interface IExchangeUpdater
	{
		Task<ExchangeUpdateResult> UpdateExchangeAsync(ExchangeConfiguration config);
		Task<IEnumerable<ExchangeUpdateResult>> UpdateExchangesAsync(IEnumerable<ExchangeConfiguration> configs);
	}
}