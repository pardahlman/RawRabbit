using System;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using RawRabbit.DependecyInjection;

namespace RawRabbit.DependencyInjection.Ninject
{
	public class NinjectAdapter : IDependecyResolver
	{
		private readonly IContext _context;

		public NinjectAdapter(IContext context)
		{
			_context = context;
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService) GetService(typeof(TService), additional);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			var args = additional
				.Select(a => new TypeMatchingConstructorArgument(a.GetType(), (context, target) => a))
				.ToArray<IParameter>();
			return _context.Kernel.Get(serviceType, args);
		}
	}
}
