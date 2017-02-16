using System;
using RawRabbit.DependecyInjection;
using RawRabbit.Logging;

namespace RawRabbit.Instantiation
{
	public class RawRabbitFactory
	{
		public static Disposable.BusClient CreateSingleton(RawRabbitOptions options = null)
		{
			var ioc = new SimpleDependecyInjection();
			return CreateSingleton(options, ioc, register => ioc);
		}

		public static Disposable.BusClient CreateSingleton(RawRabbitOptions options, IDependecyRegister register, Func<IDependecyRegister, IDependecyResolver> resolverFunc)
		{
			var factory = CreateInstanceFactory(options, register, resolverFunc);
			return new Disposable.BusClient(factory);
		}

		public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options = null)
		{
			var ioc = new SimpleDependecyInjection();
			return CreateInstanceFactory(options, ioc, register => ioc);
		}

		public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options, IDependecyRegister register, Func<IDependecyRegister, IDependecyResolver> resolverFunc)
		{
			register.AddRawRabbit();
			var resolver = resolverFunc(register);
			LogManager.CurrentFactory = resolver.GetService<ILoggerFactory>();
			return new InstanceFactory(resolver);
		}
	}
}
