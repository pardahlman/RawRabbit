using Ninject;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public static class KernelExtension
	{
		public static IKernel RegisterRawRabbit(this IKernel config, RawRabbitOptions options = null)
		{
			if (options != null)
			{
				config.Bind<RawRabbitOptions>().ToConstant(options);
			}
			config.Load<RawRabbitModule>();
			return config;
		}
	}
}
