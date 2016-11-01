using System;

namespace RawRabbit.DependecyInjection
{
	public interface IDependecyResolver
	{
		TService GetService<TService>(params object[] additional);
		object GetService(Type serviceType, params object[] additional);
	}
}