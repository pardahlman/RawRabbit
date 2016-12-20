using Ninject;
using Ninject.Modules;
using RawRabbit.DependecyInjection;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public class RawRabbitModule : NinjectModule
	{
		public override void Load()
		{
#if NETSTANDARD1_5
			KernelConfiguration
				.Bind<IDependecyResolver>()
				.ToMethod(context => new NinjectAdapter(context));

			KernelConfiguration
				.Bind<IInstanceFactory>()
				.ToMethod(context => RawRabbitFactory.CreateInstanceFactory(context.Kernel.Get<RawRabbitOptions>()))
				.InSingletonScope();

			KernelConfiguration
				.Bind<IBusClient>()
				.ToMethod(context => context.Kernel.Get<IInstanceFactory>().Create());
#endif
#if NET451
			Kernel
				.Bind<IDependecyResolver>()
				.ToMethod(context => new NinjectAdapter(context));

			Kernel
				.Bind<IInstanceFactory>()
				.ToMethod(context => RawRabbitFactory.CreateInstanceFactory(context.Kernel.Get<RawRabbitOptions>()))
				.InSingletonScope();

			Kernel
				.Bind<IBusClient>()
				.ToMethod(context => context.Kernel.Get<IInstanceFactory>().Create());
#endif
		}
	}
}
