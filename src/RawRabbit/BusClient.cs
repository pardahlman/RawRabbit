using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
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

		public Task<IPipeContext> InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg = null, CancellationToken token = default(CancellationToken))
		{
			var pipe = _pipeBuilderFactory.Create(pipeCfg);
			var context = _contextFactory.CreateContext();
			contextCfg?.Invoke(context);
			return pipe
				.InvokeAsync(context, token)
				.ContinueWith(t =>
				{
					if (t.IsCanceled)
					{
						return TaskUtil.FromCancelled<IPipeContext>();
					}
					if (t.IsFaulted)
					{
						return TaskUtil.FromException<IPipeContext>(t.Exception.InnerException);
					}
					return Task.FromResult(context);
				}, token)
				.Unwrap();
		}
	}
}
