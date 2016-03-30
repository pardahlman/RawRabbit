using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Extensions.TopologyUpdater.Core.Abstraction
{
	public interface IExchangeUpdater
	{
		Task UpdateExchangeAsync(ExchangeConfiguration config);
		Task UpdateExchangesAsync(IEnumerable<ExchangeConfiguration> configs);
	}
}