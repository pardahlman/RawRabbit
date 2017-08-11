using System;
using Microsoft.Extensions.DependencyInjection;

namespace RawRabbit.DependencyInjection.ServiceCollection
{
	public class ServiceProviderAdapter : IDependencyResolver
	{
		private readonly IServiceProvider _provider;

		public ServiceProviderAdapter(IServiceProvider provider)
		{
			_provider = provider;
		}

		public ServiceProviderAdapter(IServiceCollection collection)
		{
			collection.AddSingleton<IDependencyResolver, ServiceProviderAdapter>(provider => this);
			_provider = collection.BuildServiceProvider();
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService)GetService(typeof(TService), additional);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			additional = additional ?? new object[0];
			var service = _provider.GetService(serviceType);
			return service ?? ActivatorUtilities.CreateInstance(_provider, serviceType, additional);
		}
	}
}
