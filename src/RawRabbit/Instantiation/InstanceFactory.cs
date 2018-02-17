using System;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using RawRabbit.Subscription;

namespace RawRabbit.Instantiation
{
	public interface IInstanceFactory
	{
		IBusClient Create();
	}

	public class InstanceFactory : IDisposable, IInstanceFactory
	{
		private readonly IDependencyResolver _resolver;

		public InstanceFactory(IDependencyResolver resolver)
		{
			_resolver = resolver;
		}

		public IBusClient Create()
		{
			return new BusClient(_resolver.GetService<IPipeBuilderFactory>(), _resolver.GetService<IPipeContextFactory>(), _resolver.GetService<IChannelFactory>());
		}

		public void Dispose()
		{
			var diposer = _resolver.GetService<IResourceDisposer>();
			diposer?.Dispose();
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			var subscriptions = _resolver.GetService<ISubscriptionRepository>().GetAll();
			foreach (var subscription in subscriptions)
			{
				subscription?.Dispose();
			}
			graceful = graceful ?? _resolver.GetService<RawRabbitConfiguration>().GracefulShutdown;
			await Task.Delay(graceful.Value);
			Dispose();
		}
	}
}
