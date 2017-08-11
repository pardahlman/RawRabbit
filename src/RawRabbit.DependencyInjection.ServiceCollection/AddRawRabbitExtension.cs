using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.ServiceCollection
{
	public static class AddRawRabbitExtension
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, RawRabbitOptions options = null)
		{
			var adapter = new ServiceCollectionAdapter(collection);
			adapter.AddRawRabbit(options);
			options?.DependencyInjection?.Invoke(adapter);
			return collection;
		}
	}
}
