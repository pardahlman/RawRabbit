using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Instantiation;
using RawRabbitFactory = RawRabbit.vNext.Pipe.RawRabbitFactory;
using RawRabbitOptions = RawRabbit.vNext.Pipe.RawRabbitOptions;

namespace RawRabbit.vNext
{
	public static class AddRawRabbitExtension
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, RawRabbitOptions options = null)
		{
			collection
				.AddSingleton(c => RawRabbitFactory.CreateInstanceFactory(options, collection))
				.AddTransient(p => p.GetService<InstanceFactory>().Create());
			return collection;
		}
	}
}
