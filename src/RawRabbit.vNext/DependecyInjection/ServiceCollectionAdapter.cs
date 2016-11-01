using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependecyInjection;

namespace RawRabbit.vNext.DependecyInjection
{
	public class ServiceCollectionAdapter : IDependecyRegister
	{
		public IServiceCollection Collection { get; set; }

		public ServiceCollectionAdapter(IServiceCollection collection)
		{
			Collection = collection;
		}

		public IDependecyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			Collection.AddTransient<TService, TImplementation>();
			return this;
		}

		public IDependecyRegister AddTransient<TService>(Func<IDependecyResolver, TService> instanceCreator) where TService : class
		{
			Collection.AddTransient(c => instanceCreator(c.GetService<IDependecyResolver>()));
			return this;
		}

		public IDependecyRegister AddTransient<TService, TImplementation>(Func<IDependecyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			Collection.AddTransient<TService, TImplementation>(c => instanceCreator(c.GetService<IDependecyResolver>()));
			return this;
		}

		public IDependecyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			Collection.AddSingleton(instance);
			return this;
		}

		public IDependecyRegister AddSingleton<TService, TImplementation>(Func<IDependecyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
		{
			Collection.AddSingleton(c => instanceCreator(c.GetService<IDependecyResolver>()));
			return this;
		}

		public IDependecyRegister AddSingleton<TService>(Func<IDependecyResolver, TService> instanceCreator) where TService : class
		{
			Collection.AddSingleton<TService>(c => instanceCreator(c.GetService<IDependecyResolver>()));
			return this;
		}

		public IDependecyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			Collection.AddSingleton<TService, TImplementation>();
			return this;
		}
	}
}
