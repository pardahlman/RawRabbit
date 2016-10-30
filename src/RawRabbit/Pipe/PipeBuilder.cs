using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilder
	{
		IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler);
		IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware;
		IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, params object[] args) where TCurrent: Middleware.Middleware where TNew : Middleware.Middleware;
	}

	public interface IExtendedPipeBuilder : IPipeBuilder
	{
		Middleware.Middleware Build();
	}

	public class PipeBuilder : IExtendedPipeBuilder
	{
		protected List<MiddlewareInfo> Pipe;

		public PipeBuilder()
		{
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
			predicate = predicate ?? (objects => true);
			var matching = Pipe.Where(c => c.Type == typeof(TCurrent) && predicate(c.ConstructorArgs));
			foreach (var middlewareInfo in matching)
			{
				middlewareInfo.Type = typeof(TNew);
				middlewareInfo.ConstructorArgs = args;
			}
			return this;
		}

		public virtual Middleware.Middleware Build()
		{
			var middlewares = Pipe.Select(CreateInstance).ToList();
			return Build(middlewares);
		}

		protected virtual Middleware.Middleware Build(IList<Middleware.Middleware> middlewares)
		{
			Middleware.Middleware next = new NoOpMiddleware();
			for (var i = middlewares.Count - 1; i >= 0; i--)
			{
				var current = middlewares[i];
				current.Next = next;
				next = current;
			}
			return next;
		}

		protected virtual Middleware.Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return Activator.CreateInstance(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware.Middleware;
		}
	}
}