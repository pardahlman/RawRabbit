using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependecyInjection;
using RawRabbit.Instantiation;
using RawRabbit.vNext.DependecyInjection;
using RawRabbitOptions = RawRabbit.vNext.Pipe.RawRabbitOptions;

namespace RawRabbit.vNext
{
	public static class AddRawRabbitExtension
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, RawRabbitOptions options = null)
		{
			options?.DependencyInjection?.Invoke(collection);
			var adapter = new ServiceCollectionAdapter(collection);
			adapter.AddRawRabbit(options);
			
			return collection;
		}
	}
}
