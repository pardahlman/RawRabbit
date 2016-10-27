using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.vNext.Pipe
{
	public class PipeBuilder : RawRabbit.Pipe.PipeBuilder
	{
		private readonly IServiceProvider _provider;
		private readonly Action<IPipeBuilder> _additional;

		public PipeBuilder(IServiceProvider provider)
		{
			_provider = provider;
			_additional = provider.GetService<Action<IPipeBuilder>>();
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
			return ActivatorUtilities.CreateInstance(_provider, middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware;
		}
	}
}
