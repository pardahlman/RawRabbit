using Ninject;
using Ninject.Modules;
using RawRabbit.DependencyInjection;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public class RawRabbitModule : NinjectModule
	{
		public override void Load()
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			KernelConfiguration
				.Bind<IDependencyResolver>()
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
				.Bind<IDependencyResolver>()
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
