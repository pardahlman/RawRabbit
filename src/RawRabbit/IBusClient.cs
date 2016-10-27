using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public interface IBusClient
	{
		Task InvokeAsync(Action<IPipeBuilder> pipeCfg, CancellationToken token = default(CancellationToken));
		Task InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg, CancellationToken token = default(CancellationToken));
	}
}