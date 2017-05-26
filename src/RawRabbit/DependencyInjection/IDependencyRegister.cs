using System;

namespace RawRabbit.DependencyInjection
{
	public interface IDependencyRegister
	{
		IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator)
			where TService : class where TImplementation : class, TService;
		IDependencyRegister AddTransient<TService, TImplementation>()
			where TImplementation : class, TService where TService : class;
		IDependencyRegister AddSingleton<TService>(TService instance)
			where TService : class;
		IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator)
			where TImplementation : class, TService where TService : class;
		IDependencyRegister AddSingleton<TService, TImplementation>()
			where TImplementation : class, TService where TService : class;
	}

	public static class DependencyRegisterExtensions
	{
		public static IDependencyRegister AddTransient<TImplementation>(this IDependencyRegister register, Func<IDependencyResolver, TImplementation> instanceCreator)
			where TImplementation : class
		{
			return register.AddTransient<TImplementation, TImplementation>(instanceCreator);
		}

		public static IDependencyRegister AddSingleton<TImplementation>(this IDependencyRegister register, Func<IDependencyResolver, TImplementation> instanceCreator)
			where TImplementation : class
		{
			return register.AddSingleton<TImplementation, TImplementation>(instanceCreator);
		}
	}
}