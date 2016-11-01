using System;

namespace RawRabbit.DependecyInjection
{
	public interface IDependecyRegister
	{
		IDependecyRegister AddTransient<TService, TImplementation>(Func<IDependecyResolver, TImplementation> instanceCreator)
			where TService : class where TImplementation : class, TService;
		IDependecyRegister AddTransient<TService, TImplementation>()
			where TImplementation : class, TService where TService : class;
		IDependecyRegister AddSingleton<TService>(TService instance)
			where TService : class;
		IDependecyRegister AddSingleton<TService, TImplementation>(Func<IDependecyResolver, TService> instanceCreator)
			where TImplementation : class, TService where TService : class;
		IDependecyRegister AddSingleton<TService, TImplementation>()
			where TImplementation : class, TService where TService : class;
	}

	public static class DependecyRegisterExtensions
	{
		public static IDependecyRegister AddTransient<TImplementation>(this IDependecyRegister register, Func<IDependecyResolver, TImplementation> instanceCreator)
			where TImplementation : class
		{
			return register.AddTransient<TImplementation, TImplementation>(instanceCreator);
		}

		public static IDependecyRegister AddSingleton<TImplementation>(this IDependecyRegister register, Func<IDependecyResolver, TImplementation> instanceCreator)
			where TImplementation : class
		{
			return register.AddSingleton<TImplementation, TImplementation>(instanceCreator);
		}
	}
}