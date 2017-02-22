using Autofac;
using Autofac.Features.ResolveAnything;
using RawRabbit.DependecyInjection;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Autofac
{
	public static class ContainerBuilderExtension
	{
		private const string RawRabbit = "RawRabbit";

		public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, RawRabbitOptions options = null)
		{
			builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource(type => type.Namespace.StartsWith(RawRabbit)));
			var adapter = new ContainerBuilderAdapter(builder);
			adapter.AddRawRabbit(options);
			return builder;
		}
	}
}
