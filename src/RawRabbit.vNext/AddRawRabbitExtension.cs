using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependencyInjection;
using RawRabbit.vNext.DependencyInjection;
using RawRabbitOptions = RawRabbit.vNext.Pipe.RawRabbitOptions;

namespace RawRabbit.vNext
{
	public static class AddRawRabbitExtension
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, RawRabbitOptions options = null)
		{
			var adapter = new ServiceCollectionAdapter(collection);
			adapter.AddRawRabbit(options);
			options?.DependencyInjection?.Invoke(collection);
			return collection;
		}
	}
}
