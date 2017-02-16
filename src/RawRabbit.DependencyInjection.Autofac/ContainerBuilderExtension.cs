using Autofac;
using RawRabbit.DependecyInjection;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Autofac
{
	public static class ContainerBuilderExtension
	{
		public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, RawRabbitOptions options = null)
		{
			var adapter = new ContainerBuilderAdapter(builder);
			adapter.AddRawRabbit(options);
			return builder;
		}
	}
}
