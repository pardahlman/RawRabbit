using System;
using Microsoft.Extensions.DependencyInjection;

namespace RawRabbit.DependencyInjection.ServiceCollection
{
	public class ServiceCollectionAdapter : IDependencyRegister
	{
		public IServiceCollection Collection { get; set; }

		public ServiceCollectionAdapter(IServiceCollection collection)
		{
			Collection = collection;
		}

		public IDependencyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			Collection.AddTransient<TService, TImplementation>();
			return this;
		}

		public IDependencyRegister AddTransient<TService>(Func<IDependencyResolver, TService> instanceCreator) where TService : class
		{
			Collection.AddTransient(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			Collection.AddTransient<TService, TImplementation>(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			Collection.AddSingleton(instance);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
		{
			Collection.AddSingleton(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(Func<IDependencyResolver, TService> instanceCreator) where TService : class
		{
			Collection.AddSingleton<TService>(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			Collection.AddSingleton<TService, TImplementation>();
			return this;
		}
	}
}
