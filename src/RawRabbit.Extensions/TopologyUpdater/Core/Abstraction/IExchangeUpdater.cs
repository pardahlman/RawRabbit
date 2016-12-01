using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core.Abstraction
{
	public interface IExchangeUpdater
	{
		Task<ExchangeUpdateResult> UpdateExchangeAsync(ExchangeUpdateDeclaration config);
		Task<IEnumerable<ExchangeUpdateResult>> UpdateExchangesAsync(IEnumerable<ExchangeUpdateDeclaration> configs);
	}
}