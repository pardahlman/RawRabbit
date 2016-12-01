using System;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Extensions.TopologyUpdater.Model
{
	public class ExchangeUpdateDeclaration : ExchangeDeclaration
	{
		public Func<string, string> BindingTransformer { get; set; }

		public ExchangeUpdateDeclaration(GeneralExchangeConfiguration exchange) : base(exchange)
		{
			BindingTransformer = s => s;
		}

		public ExchangeUpdateDeclaration()
		{
			BindingTransformer = s => s;
		}
	}
}
