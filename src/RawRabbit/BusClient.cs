using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public class BusClient : IBusClient
	{
		private readonly IPipeBuilderFactory _pipeBuilderFactory;
		private readonly IPipeContextFactory _contextFactory;

		public BusClient(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory)
		{
			_pipeBuilderFactory = pipeBuilderFactory;
			_contextFactory = contextFactory;
		}

		public Task InvokeAsync(Action<IPipeBuilder> pipeCfg, CancellationToken token)
		{
			return InvokeAsync(pipeCfg, context => { }, token);
		}

		public Task InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg, CancellationToken token = new CancellationToken())
		{
			var builder = _pipeBuilderFactory.Create();
			pipeCfg(builder);
			var pipe = builder.Build();
			var context = _contextFactory.CreateContext();
			contextCfg(context);
			return pipe.InvokeAsync(context);
		}
	}
}
