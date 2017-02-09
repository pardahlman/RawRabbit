using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilder
	{
		IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler);
		IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware;
		IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, Func<object[], object[]> argsFunc = null) where TCurrent: Middleware.Middleware where TNew : Middleware.Middleware;
		IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, params object[] args) where TCurrent: Middleware.Middleware where TNew : Middleware.Middleware;
		IPipeBuilder Remove<TMiddleware>(Predicate<object[]> predicate = null) where TMiddleware : Middleware.Middleware;
	}

	public interface IExtendedPipeBuilder : IPipeBuilder
	{
		Middleware.Middleware Build();
	}

	public class PipeBuilder : IExtendedPipeBuilder
	{
		private readonly IDependecyResolver _resolver;
		protected List<MiddlewareInfo> Pipe;
		private readonly Action<IPipeBuilder> _additional;

		public PipeBuilder(IDependecyResolver resolver)
		{
			_resolver = resolver;
			_additional = _resolver.GetService<Action<IPipeBuilder>>() ?? (builder => {});
			Pipe = new List<MiddlewareInfo>();
		}

		public IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler)
		{
			Use<UseHandlerMiddleware>(handler);
			return this;
		}

		public IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware
		{
			Pipe.Add(new MiddlewareInfo
			{
				Type = typeof(TMiddleWare),
				ConstructorArgs = args
			});
			return this;
		}

		public IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, params object[] args) where TCurrent : Middleware.Middleware where TNew : Middleware.Middleware
		{
			return Replace<TCurrent, TNew>(predicate, oldArgs => args);
;		}

		public IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, Func<object[], object[]> argsFunc = null) where TCurrent : Middleware.Middleware where TNew : Middleware.Middleware
		{
			predicate = predicate ?? (objects => true);
			var matching = Pipe.Where(c => c.Type == typeof(TCurrent) && predicate(c.ConstructorArgs));
			foreach (var middlewareInfo in matching)
			{
				var args = argsFunc?.Invoke(middlewareInfo.ConstructorArgs);
				middlewareInfo.Type = typeof(TNew);
				middlewareInfo.ConstructorArgs = args;
			}
			return this;
		}

		public IPipeBuilder Remove<TMiddleware>(Predicate<object[]> predicate = null) where TMiddleware : Middleware.Middleware
		{
			predicate = predicate ?? (objects => true);
			var matching = Pipe.Where(c => c.Type == typeof(TMiddleware) && predicate(c.ConstructorArgs)).ToList();
			foreach (var match in matching)
			{
				Pipe.Remove(match);
			}
			return this;
		}

		public virtual Middleware.Middleware Build()
		{
			_additional.Invoke(this);

			var stageMarkerOptions = Pipe
				.Where(info => info.Type == typeof(StageMarkerMiddleware))
				.SelectMany(info => info.ConstructorArgs.OfType<StageMarkerOptions>());

			var stageMwInfo = Pipe
				.Where(info => typeof(StagedMiddleware).GetTypeInfo().IsAssignableFrom(info.Type))
				.ToList();
			var stagedMiddleware = stageMwInfo
				.Select(CreateInstance)
				.OfType<StagedMiddleware>()
				.ToList();

			foreach (var stageMarkerOption in stageMarkerOptions)
			{
				var thisStageMws = stagedMiddleware.Where(mw => mw.StageMarker == stageMarkerOption.Stage).ToList<Middleware.Middleware>();
				stageMarkerOption.EntryPoint = Build(thisStageMws);
			}
			Pipe = Pipe.Except(stageMwInfo).ToList();
			var middlewares = Pipe.Select(CreateInstance).ToList();
			return Build(middlewares);
		}

		protected virtual Middleware.Middleware Build(IList<Middleware.Middleware> middlewares)
		{
			Middleware.Middleware next = new NoOpMiddleware();
			for (var i = middlewares.Count - 1; i >= 0; i--)
			{
				Middleware.Middleware cancellation = new CancellationMiddleware();
				var current = middlewares[i];
				current.Next = cancellation;
				cancellation.Next = next;
				next = current;
			}
			return next;
		}

		protected virtual Middleware.Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return _resolver.GetService(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware.Middleware;
		}
	}
}