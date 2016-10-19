using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe
{
	public interface IPipeBuilder
	{
		IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler);
		IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware;
	}

	public interface IExtendedPipeBuilder : IPipeBuilder
	{
		Middleware.Middleware Build();
	}

	public class PipeBuilder : IExtendedPipeBuilder
	{
		private readonly List<MiddlewareInfo> _pipe;

		public PipeBuilder()
		{
			_pipe = new List<MiddlewareInfo>();
		}

		public IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler)
		{
			Use<UseHandlerMiddleware>(handler);
			return this;
		}

		public IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware
		{
			_pipe.Add(new MiddlewareInfo
			{
				Type = typeof(TMiddleWare),
				ConstructorArgs = args
			});
			return this;
		}
		
		public Middleware.Middleware Build()
		{
			Middleware.Middleware next = new NoOpMiddleware();
			for (var i = _pipe.Count-1; i >= 0; i--)
			{
				var current = CreateInstance(_pipe[i]);
				current.Next = next;
				next = current;
			}
			return next;
		}

		protected virtual Middleware.Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return  Activator.CreateInstance(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware.Middleware;
		}
	}
}