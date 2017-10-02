using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Instantiation.Disposable
{
	/// <summary>
	/// Disposable implementation of IBusClient. This implementation will dispose all dependencies when it is deposed,
	/// including channels, consumers and connection(s).
	/// </summary>
	public class BusClient : IBusClient, IDisposable
	{
		private readonly IInstanceFactory _instanceFactory;
		private readonly IBusClient _busClient;

		public BusClient(IInstanceFactory instanceFactory)
		{
			_instanceFactory = instanceFactory;
			_busClient = instanceFactory.Create();
		}

		public Task<IPipeContext> InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg, CancellationToken token = new CancellationToken())
		{
			return _busClient.InvokeAsync(pipeCfg, contextCfg, token);
		}

		public void Dispose()
		{
			(_instanceFactory as IDisposable)?.Dispose();
		}
	}
}
