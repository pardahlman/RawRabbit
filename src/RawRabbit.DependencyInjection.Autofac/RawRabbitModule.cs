using Autofac;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Autofac
{
	public class RawRabbitModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(AutofacAdapter.Create)
				.SingleInstance()
				.AsImplementedInterfaces();

			builder
				.Register(context => RawRabbitFactory
					.CreateInstanceFactory(
						context.IsRegistered(typeof(RawRabbitOptions))
							? context.Resolve<RawRabbitOptions>()
							: null
						)
					)
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.Register(context => context.Resolve<IInstanceFactory>().Create())
				.InstancePerLifetimeScope();
		}
	}
}
