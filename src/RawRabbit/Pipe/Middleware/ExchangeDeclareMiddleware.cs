using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeclareOptions
	{
		public Func<IPipeContext, ExchangeConfiguration> ExchangeFunc { get; set; }

		public static ExchangeDeclareOptions For(ExchangeConfiguration cfg)
		{
			return new ExchangeDeclareOptions
			{
				ExchangeFunc = context => cfg
			};
		}

		public static ExchangeDeclareOptions For(Func<IPipeContext, ExchangeConfiguration> func)
		{
			return new ExchangeDeclareOptions
			{
				ExchangeFunc = func
			};
		}
	}

	public class ExchangeDeclareMiddleware : Middleware
	{
		private readonly ITopologyProvider _topologyProvider;
		private readonly Func<IPipeContext, ExchangeConfiguration> _exchangeFunc;

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider)
			: this(topologyProvider, ExchangeDeclareOptions.For(c => c.GetExchangeConfiguration()))
		{ }

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options)
		{
			_topologyProvider = topologyProvider;
			_exchangeFunc = options.ExchangeFunc;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var exchangeCfg = _exchangeFunc(context);

			return _topologyProvider
				.DeclareExchangeAsync(exchangeCfg)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
