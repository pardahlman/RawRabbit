using System;
using System.Collections.Generic;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Extensions.TopologyUpdater.Model
{
    public class ExchangeUpdateResult
    {
        public ExchangeConfiguration Exchange { get; set; }
        public List<Binding> Bindings { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}
