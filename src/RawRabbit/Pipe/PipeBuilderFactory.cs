using System;
using System.Collections.Concurrent;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilderFactory
	{
		IExtendedPipeBuilder Create();
		Middleware.Middleware Create(Action<IPipeBuilder> pipe);
	}

	public class PipeBuilderFactory : IPipeBuilderFactory
	{
		private readonly Func<IExtendedPipeBuilder> _pipeBuilder;

		public PipeBuilderFactory(Func<IExtendedPipeBuilder> pipeBuilder)
		{
			_pipeBuilder = pipeBuilder;
		}

		public IExtendedPipeBuilder Create()
		{
			return _pipeBuilder();
		}

		public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
		{
			var builder = _pipeBuilder();
			pipe(builder);
			return builder.Build();
		}
	}

	public class CachedPipeBuilderFactory : IPipeBuilderFactory
	{
		private readonly PipeBuilderFactory _fallback;
		private readonly ConcurrentDictionary<Action<IPipeBuilder>, Middleware.Middleware> _pipeCache;

		public CachedPipeBuilderFactory(Func<IExtendedPipeBuilder> pipeBuilder)
		{
			_fallback = new PipeBuilderFactory(pipeBuilder);
			_pipeCache = new ConcurrentDictionary<Action<IPipeBuilder>, Middleware.Middleware>();
		}

		public IExtendedPipeBuilder Create()
		{
			return _fallback.Create();
		}

		public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
		{
			var result = _pipeCache.GetOrAdd(pipe, action => _fallback.Create(action));
			return result;
		}
	}
}
