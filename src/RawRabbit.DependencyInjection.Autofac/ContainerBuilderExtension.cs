using Autofac;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Autofac
{
	public static class ContainerBuilderExtension
	{
		public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, RawRabbitOptions options = null)
		{
			if (options != null)
			{
				builder.Register(context => options);
			}

			builder.RegisterModule<RawRabbitModule>();
			return builder;
		}
	}
}
