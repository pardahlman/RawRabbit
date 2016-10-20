using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Client
{
	public interface IBusClient
	{
		Task InvokeAsync(Action<IPipeBuilder> pipe, CancellationToken token = default(CancellationToken));
	}

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
			var builder = _pipeBuilderFactory.Create();
			pipeCfg(builder);
			var pipe = builder.Build();
			return pipe.InvokeAsync(_contextFactory.CreateContext());
		}
	}
}
