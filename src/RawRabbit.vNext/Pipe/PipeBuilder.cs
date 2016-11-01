using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.vNext.Pipe
{
	public class PipeBuilder : RawRabbit.Pipe.PipeBuilder
	{
		private readonly IDependecyResolver _resolver;
		private readonly Action<IPipeBuilder> _additional;

		public PipeBuilder(IDependecyResolver resolver) : base(resolver)
		{
			_resolver = resolver;
			_additional = _resolver.GetService<Action<IPipeBuilder>>();
		}

		public override Middleware Build()
		{
			_additional?.Invoke(this);

			var stageMarkerOptions = Pipe
				.Where(info => info.Type == typeof(StageMarkerMiddleware))
				.SelectMany(info => info.ConstructorArgs.OfType<StageMarkerOptions>());

			var stageMwInfo = Pipe.Where(info => typeof(StagedMiddleware).IsAssignableFrom(info.Type));
			var stagedMiddleware = stageMwInfo
				.Select(CreateInstance)
				.OfType<StagedMiddleware>()
				.ToList();

			foreach (var stageMarkerOption in stageMarkerOptions)
			{
				var thisStageMws = stagedMiddleware.Where(mw => mw.StageMarker == stageMarkerOption.Stage).ToList<Middleware>();
				stageMarkerOption.EntryPoint = Build(thisStageMws);
			}
			Pipe = Pipe.Except(stageMwInfo).ToList();
			return base.Build();
		}

		protected override Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return _resolver.GetService(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware;
		}
	}
}
