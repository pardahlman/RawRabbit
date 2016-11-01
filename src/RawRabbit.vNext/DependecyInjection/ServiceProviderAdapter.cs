﻿using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependecyInjection;

namespace RawRabbit.vNext.DependecyInjection
{
	public class ServiceProviderAdapter : IDependecyResolver
	{
		private readonly IServiceProvider _provider;

		public ServiceProviderAdapter(IServiceProvider provider)
		{
			_provider = provider;
		}

		public ServiceProviderAdapter(IServiceCollection collection)
		{
			collection.AddSingleton<IDependecyResolver, ServiceProviderAdapter>(provider => this);
			_provider = collection.BuildServiceProvider();
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService)GetService(typeof(TService), additional);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			var service = _provider.GetService(serviceType);
			return service ?? ActivatorUtilities.CreateInstance(_provider, serviceType, additional);
		}
	}
}