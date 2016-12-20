using Ninject;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public static class KernelExtension
	{
#if NETSTANDARD1_5
		public static IKernelConfiguration RegisterRawRabbit(this IKernelConfiguration config, RawRabbitOptions options = null)
		{
			if (options != null)
			{
				config.Bind<RawRabbitOptions>().ToConstant(options);
			}
			config.Load<RawRabbitModule>();
			return config;
		}
#endif
#if NET451
		public static IKernel RegisterRawRabbit(this IKernel config, RawRabbitOptions options = null)
		{
			if (options != null)
			{
				config.Bind<RawRabbitOptions>().ToConstant(options);
			}
			config.Load<RawRabbitModule>();
			return config;
		}
#endif
	}
}
