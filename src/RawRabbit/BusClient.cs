using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public class BusClient : IBusClient
	{
		private readonly IPipeBuilderFactory _pipeBuilderFactory;
		private readonly IPipeContextFactory _contextFactory;

		public BusClient(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, IChannelFactory factory)
		{
			_pipeBuilderFactory = pipeBuilderFactory;
			_contextFactory = contextFactory;
		}

		public async Task<IPipeContext> InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg = null, CancellationToken token = default(CancellationToken))
		{
			var pipe = _pipeBuilderFactory.Create(pipeCfg);
			var context = _contextFactory.CreateContext();
			contextCfg?.Invoke(context);
			await pipe.InvokeAsync(context, token);
			return context;
		}
	}
}
