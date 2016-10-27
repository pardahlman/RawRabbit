using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Context;
using RawRabbit.Extensions.Disposable;
using RawRabbit.vNext;

namespace RawRabbit.Extensions.Client
{
	public class RawRabbitFactory
	{
		[Obsolete("Use 'Create' methods instead.")]
		public static ILegacyBusClient<MessageContext> GetExtendableClient(Action<IServiceCollection> custom = null)
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(config: null, custom: custom)
				.AddRawRabbitExtensions<MessageContext>()
				.BuildServiceProvider();
			return new ExtendableBusClient(provider);
		}

		public static Disposable.ILegacyBusClient<TMessageContext> Create<TMessageContext>(Action<IServiceCollection> custom = null)
			where TMessageContext : IMessageContext
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(config: null, custom: custom)
				.AddRawRabbitExtensions<MessageContext>()
				.BuildServiceProvider();
			return new LegacyBusClient<TMessageContext>(new ExtendableBusClient<TMessageContext>(provider));
		}

		public static Disposable.ILegacyBusClient Create(Action<IServiceCollection> custom = null)
		{
			var provider = new ServiceCollection()
				.AddRawRabbit(config: null, custom: custom)
				.AddRawRabbitExtensions<MessageContext>()
				.BuildServiceProvider();
			return new Disposable.LegacyBusClient(new ExtendableBusClient(provider));
		}
	}
}
