﻿using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using RawRabbit.Subscription;

namespace RawRabbit.Instantiation
{
	public interface IInstanceFactory : IDisposable
	{
		IBusClient Create();
	}

	public class InstanceFactory : IInstanceFactory
	{
		private readonly IDependencyResolver _resolver;

		public InstanceFactory(IDependencyResolver resolver)
		{
			_resolver = resolver;
		}

		public IBusClient Create()
		{
			return new BusClient(_resolver.GetService<IPipeBuilderFactory>(), _resolver.GetService<IPipeContextFactory>());
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
