using System;
using System.Collections.Concurrent;
using RawRabbit.DependecyInjection;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilderFactory
	{
		IExtendedPipeBuilder Create();
		Middleware.Middleware Create(Action<IPipeBuilder> pipe);
	}

	public class PipeBuilderFactory : IPipeBuilderFactory
	{
		private readonly IDependecyResolver _resolver;

		public PipeBuilderFactory(IDependecyResolver resolver)
		{
			_resolver = resolver;
		}

		public IExtendedPipeBuilder Create()
		{
			return new PipeBuilder(_resolver);
		}

		public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
		{
			var builder = Create();
			pipe(builder);
			return builder.Build();
		}
	}

	public class CachedPipeBuilderFactory : IPipeBuilderFactory
	{
		private readonly PipeBuilderFactory _fallback;
		private readonly ConcurrentDictionary<Action<IPipeBuilder>, Middleware.Middleware> _pipeCache;

		public CachedPipeBuilderFactory(IDependecyResolver resolver)
		{
			_fallback = new PipeBuilderFactory(resolver);
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
