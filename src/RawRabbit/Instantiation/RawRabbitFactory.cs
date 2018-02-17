using System;
using RawRabbit.DependencyInjection;

namespace RawRabbit.Instantiation
{
	public class RawRabbitFactory
	{
		public static Disposable.BusClient CreateSingleton(RawRabbitOptions options = null)
		{
			var ioc = new SimpleDependencyInjection();
			return CreateSingleton(options, ioc, register => ioc);
		}

		public static Disposable.BusClient CreateSingleton(RawRabbitOptions options, IDependencyRegister register, Func<IDependencyRegister, IDependencyResolver> resolverFunc)
		{
			var factory = CreateInstanceFactory(options, register, resolverFunc);
			return new Disposable.BusClient(factory);
		}

		public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options = null)
		{
			var ioc = new SimpleDependencyInjection();
			return CreateInstanceFactory(options, ioc, register => ioc);
		}

		public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options, IDependencyRegister register, Func<IDependencyRegister, IDependencyResolver> resolverFunc)
		{
			register.AddRawRabbit(options);
			var resolver = resolverFunc(register);
			return new InstanceFactory(resolver);
		}
	}
}
