using System;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Extensions.TopologyUpdater.Model
{
	public class ExchangeUpdateConfiguration : ExchangeConfiguration
	{
		public Func<string, string> BindingTransformer { get; set; }

		public ExchangeUpdateConfiguration(GeneralExchangeConfiguration exchange) : base(exchange)
		{
			BindingTransformer = s => s;
		}

		public ExchangeUpdateConfiguration()
		{
			BindingTransformer = s => s;
		}
	}
}
