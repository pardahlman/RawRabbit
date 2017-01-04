using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeclareOptions
	{
		public Func<IPipeContext, ExchangeDeclaration> ExchangeFunc { get; set; }
		public bool ThrowOnFail { get; set; }

		public static ExchangeDeclareOptions For(Func<IPipeContext, ExchangeDeclaration> func)
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
		private readonly Func<IPipeContext, ExchangeDeclaration> _exchangeFunc;
		private readonly bool _throwOnFail;
		private readonly ILogger _logger = LogManager.GetLogger<ExchangeDeclareMiddleware>();

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider)
			: this(topologyProvider, ExchangeDeclareOptions.For(c => c.GetExchangeDeclaration()))
		{ }

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options)
		{
			_topologyProvider = topologyProvider;
			_exchangeFunc = options.ExchangeFunc;
			_throwOnFail = (bool) options?.ThrowOnFail;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var exchangeCfg = _exchangeFunc(context);

			if (exchangeCfg != null)
			{
				_logger.LogDebug($"Exchange configuration found. Declaring '{exchangeCfg.Name}'.");
				return _topologyProvider
					.DeclareExchangeAsync(exchangeCfg)
					.ContinueWith(t => Next.InvokeAsync(context, token), token)
					.Unwrap();
			}

			_logger.LogDebug($"No Exchange configuration found. Throw on fail: {_throwOnFail}");
			if (_throwOnFail)
			{
				throw new ArgumentNullException(nameof(exchangeCfg));
			}
			return Next.InvokeAsync(context, token);
		}
	}
}