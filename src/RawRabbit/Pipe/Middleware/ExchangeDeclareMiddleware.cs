using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeclareMiddleware : Middleware
	{
		private readonly ITopologyProvider _topologyProvider;

		public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider)
		{
			_topologyProvider = topologyProvider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var exchangeCcfg = context.GetExchangeConfiguration();
			return _topologyProvider
				.DeclareExchangeAsync(exchangeCcfg)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}
