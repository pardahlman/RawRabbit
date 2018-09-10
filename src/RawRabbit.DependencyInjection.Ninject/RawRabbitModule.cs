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
		}
	}
}
