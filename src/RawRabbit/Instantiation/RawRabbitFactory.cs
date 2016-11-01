using System;
using RawRabbit.Configuration;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe;

namespace RawRabbit.Instantiation
{
	public class RawRabbitFactory
	{
		public static IBusClient Create(RawRabbitOptions options = null)
		{
			var ioc = new SimpleDependecyInjection();
			return Create(options, ioc, register => ioc);
		}

		public static IBusClient Create(RawRabbitOptions options, IDependecyRegister register, Func<IDependecyRegister, IDependecyResolver> resolverFunc)
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

			var resolver = resolverFunc(register);
			var pipeBuliderFactory = new PipeBuilderFactory(() => new PipeBuilder(resolver));
			return new BusClient(pipeBuliderFactory, resolver.GetService<IPipeContextFactory>());
		}
	}
}
