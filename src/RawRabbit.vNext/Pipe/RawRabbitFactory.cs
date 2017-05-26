using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;
using RawRabbit.vNext.DependencyInjection;

namespace RawRabbit.vNext.Pipe
{
	public static class RawRabbitFactory
	{
		public static Instantiation.Disposable.BusClient CreateSingleton(RawRabbitOptions options = null)
		{
			var factory = CreateInstanceFactory(options);
			return new Instantiation.Disposable.BusClient(factory);
		}

		public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options = null, IServiceCollection applicationCollection = null)
		{
			var collection = applicationCollection ?? new ServiceCollection();
			var ioc = new ServiceCollectionAdapter(collection);

			Action<IDependencyRegister> di = register =>
			{
				options?.DependencyInjection?.Invoke(collection);
			};
			return Instantiation.RawRabbitFactory.CreateInstanceFactory(new Instantiation.RawRabbitOptions
			{
				Plugins = options?.Plugins,
				DependencyInjection = di,
				ClientConfiguration = options?.ClientConfiguration
			}, ioc, register => new ServiceProviderAdapter((register as ServiceCollectionAdapter)?.Collection));
		}
	}
}
