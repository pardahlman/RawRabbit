using RawRabbit.DependecyInjection;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.vNext.Pipe
{
	public class PipeBuilder : RawRabbit.Pipe.PipeBuilder
	{
		private readonly IDependecyResolver _resolver;

		public PipeBuilder(IDependecyResolver resolver) : base(resolver)
		{
			_resolver = resolver;
		}

		protected override Middleware CreateInstance(MiddlewareInfo middlewareInfo)
		{
			return _resolver.GetService(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware;
		}
	}
}
