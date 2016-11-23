using System;

namespace RawRabbit.Extensions.TopologyUpdater.Configuration.Abstraction
{
    public interface ITopologySelector
    {
        IExchangeUpdateBuilder ForExchange(string name);
        IExchangeUpdateBuilder ExchangeForMessage<TMessage>();
        ITopologySelector UseConventionForExchange<TMessage>();
        ITopologySelector UseConventionForExchange(params Type[] messageTypes);
    }
}
