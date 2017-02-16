using System;
using System.Linq;
using Autofac;
using Autofac.Core;
using RawRabbit.DependecyInjection;

namespace RawRabbit.DependencyInjection.Autofac
{
	public class ComponentContextAdapter : IDependecyResolver
	{
		public static ComponentContextAdapter Create(IComponentContext context)
		{
			return new ComponentContextAdapter(context);
		}

		private readonly IComponentContext _context;

		public ComponentContextAdapter(IComponentContext context)
		{
			_context = context;
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService)GetService(typeof(TService), additional);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			var parameters = additional
				.Select(a => new TypedParameter(a.GetType(), a))
				.ToList<Parameter>();
			return _context.Resolve(serviceType, parameters);
		}
	}
}
