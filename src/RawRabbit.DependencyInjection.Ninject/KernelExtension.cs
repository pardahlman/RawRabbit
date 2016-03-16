using Ninject;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;

namespace RawRabbit.DependencyInjection.Ninject
{
	public static class KernelExtension
	{
		public static IKernel RegisterRawRabbit<TMessageContext>(this IKernel kernel)
			where TMessageContext : IMessageContext
		{
			kernel.Load(new RawRabbitModule<TMessageContext>());
			return kernel;
		}

		public static IKernel RegisterRawRabbit(this IKernel kernel)
		{
			kernel.Load(new RawRabbitModule());
			return kernel;
		}

		public static IKernel RegisterRawRabbit<TMessageContext>(this IKernel kernel, RawRabbitConfiguration configuration)
			where TMessageContext : IMessageContext
		{
			kernel
				.Bind<RawRabbitConfiguration>()
				.ToConstant(configuration)
				.InSingletonScope();

			kernel.Load(new RawRabbitModule<TMessageContext>());
			return kernel;
		}

		public static IKernel RegisterRawRabbit(this IKernel kernel, RawRabbitConfiguration configuration)
		{
			return RegisterRawRabbit<MessageContext>(kernel, configuration);
		}

		public static IKernel RegisterRawRabbit<TMessageContext>(this IKernel kernel, string connectionString)
			where TMessageContext : IMessageContext
		{
			var config = ConnectionStringParser.Parse(connectionString);
			return RegisterRawRabbit<TMessageContext>(kernel, config);
		}

		public static IKernel RegisterRawRabbit(this IKernel kernel, string connectionString)
		{
			return RegisterRawRabbit<MessageContext>(kernel, connectionString);
		}
	}
}
