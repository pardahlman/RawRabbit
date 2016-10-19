using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.vNext.Pipe
{
	public class PipeBuilder : RawRabbit.Pipe.PipeBuilder
	{
		private readonly IServiceProvider _provider;

		public PipeBuilder(IServiceProvider provider)
		{
			_provider = provider;
		}

		protected override Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return ActivatorUtilities.CreateInstance(_provider, middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware;
		}
	}
}
