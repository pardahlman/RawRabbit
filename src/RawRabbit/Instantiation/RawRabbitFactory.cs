using System;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe;

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
			options?.DependencyInjection?.Invoke(register);

			if (options?.Plugins != null)
			{
				var clientBuilder = new ClientBuilder();
				options.Plugins(clientBuilder);
				clientBuilder.ServiceAction(register);
				register.AddSingleton<IPipeBuilder, PipeBuilder>();
				register.AddSingleton(clientBuilder.PipeBuilderAction);
			}
			else
			{
				register.AddSingleton(new Action<IPipeBuilder>(b => { }));
			}
			var resolver = resolverFunc(register);
			return new InstanceFactory(resolver);
		}
	}
}
